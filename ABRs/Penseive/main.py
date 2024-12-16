import os
import numpy as np
import tensorflow as tf
import a3c  # Assuming you have the actor-critic code saved in a3c.py
import load_trace
import env

# Hyperparameters
GAMMA = 0.99
ACTOR_LR_RATE = 0.0001
CRITIC_LR_RATE = 0.001
NUM_AGENTS = 4
TRAIN_SEQ_LEN = 100
MODEL_SAVE_INTERVAL = 100
SUMMARY_DIR = './results'
LOG_FILE = './results/log'
TRAIN_TRACES = './cooked_traces/'
NN_MODEL = None


# Training and saving the model
def central_agent(net_params_queues, exp_queues):
    assert len(net_params_queues) == NUM_AGENTS
    assert len(exp_queues) == NUM_AGENTS

    with tf.Session() as sess:
        actor = a3c.ActorNetwork(sess,
                                 state_dim=[a3c.S_INFO, a3c.S_LEN], action_dim=a3c.A_DIM,
                                 learning_rate=ACTOR_LR_RATE)
        critic = a3c.CriticNetwork(sess,
                                   state_dim=[a3c.S_INFO, a3c.S_LEN],
                                   learning_rate=CRITIC_LR_RATE)

        sess.run(tf.global_variables_initializer())
        saver = tf.train.Saver()  # To save the model

        # Restore model if it exists
        if NN_MODEL is not None:
            saver.restore(sess, NN_MODEL)
            print("Model restored.")

        epoch = 0

        while True:
            # Synchronize network parameters with agents
            actor_net_params = actor.get_network_params()
            critic_net_params = critic.get_network_params()
            for i in range(NUM_AGENTS):
                net_params_queues[i].put([actor_net_params, critic_net_params])

            # Assemble experiences from agents
            total_reward = 0.0
            total_td_loss = 0.0
            total_batch_len = 0.0
            total_agents = 0
            actor_gradient_batch = []
            critic_gradient_batch = []

            for i in range(NUM_AGENTS):
                s_batch, a_batch, r_batch, terminal, info = exp_queues[i].get()

                actor_gradient, critic_gradient, td_batch = a3c.compute_gradients(
                    s_batch=np.stack(s_batch, axis=0),
                    a_batch=np.vstack(a_batch),
                    r_batch=np.vstack(r_batch),
                    terminal=terminal, actor=actor, critic=critic)

                actor_gradient_batch.append(actor_gradient)
                critic_gradient_batch.append(critic_gradient)

                total_reward += np.sum(r_batch)
                total_td_loss += np.sum(td_batch)
                total_batch_len += len(r_batch)
                total_agents += 1

            # Apply gradients
            for i in range(len(actor_gradient_batch)):
                actor.apply_gradients(actor_gradient_batch[i])
                critic.apply_gradients(critic_gradient_batch[i])

            # Log training information
            epoch += 1
            avg_reward = total_reward / total_agents
            avg_td_loss = total_td_loss / total_batch_len

            print(f'Epoch: {epoch}, Avg TD Loss: {avg_td_loss}, Avg Reward: {avg_reward}')

            if epoch % MODEL_SAVE_INTERVAL == 0:
                # Save the model
                save_path = saver.save(sess, os.path.join(SUMMARY_DIR, f"nn_model_ep_{epoch}.ckpt"))
                print(f"Model saved in file: {save_path}")


# Worker Agent to Interact with the Environment
def agent(agent_id, all_cooked_time, all_cooked_bw, net_params_queue, exp_queue):
    net_env = env.Environment(all_cooked_time=all_cooked_time, all_cooked_bw=all_cooked_bw, random_seed=agent_id)

    with tf.Session() as sess:
        actor = a3c.ActorNetwork(sess,
                                 state_dim=[a3c.S_INFO, a3c.S_LEN], action_dim=a3c.A_DIM,
                                 learning_rate=ACTOR_LR_RATE)
        critic = a3c.CriticNetwork(sess,
                                   state_dim=[a3c.S_INFO, a3c.S_LEN],
                                   learning_rate=CRITIC_LR_RATE)

        actor_net_params, critic_net_params = net_params_queue.get()
        actor.set_network_params(actor_net_params)
        critic.set_network_params(critic_net_params)

        last_bit_rate = a3c.DEFAULT_QUALITY
        bit_rate = a3c.DEFAULT_QUALITY

        s_batch = [np.zeros((a3c.S_INFO, a3c.S_LEN))]
        a_batch = [np.zeros(a3c.A_DIM)]
        r_batch = []
        entropy_record = []

        while True:
            delay, sleep_time, buffer_size, rebuf, video_chunk_size, next_video_chunk_sizes, end_of_video, video_chunk_remain = net_env.get_video_chunk(bit_rate)

            reward = a3c.VIDEO_BIT_RATE[bit_rate] / a3c.M_IN_K - a3c.REBUF_PENALTY * rebuf - a3c.SMOOTH_PENALTY * np.abs(a3c.VIDEO_BIT_RATE[bit_rate] - a3c.VIDEO_BIT_RATE[last_bit_rate]) / a3c.M_IN_K
            r_batch.append(reward)

            last_bit_rate = bit_rate

            state = np.array(s_batch[-1], copy=True)
            state = np.roll(state, -1, axis=1)

            state[0, -1] = a3c.VIDEO_BIT_RATE[bit_rate] / float(np.max(a3c.VIDEO_BIT_RATE))
            state[1, -1] = buffer_size / a3c.BUFFER_NORM_FACTOR
            state[2, -1] = float(video_chunk_size) / float(delay) / a3c.M_IN_K
            state[3, -1] = float(delay) / a3c.M_IN_K / a3c.BUFFER_NORM_FACTOR

            action_prob = actor.predict(np.reshape(state, (1, a3c.S_INFO, a3c.S_LEN)))
            bit_rate = np.argmax(action_prob)

            if len(r_batch) >= TRAIN_SEQ_LEN or end_of_video:
                exp_queue.put([s_batch[1:], a_batch[1:], r_batch[1:], end_of_video, {'entropy': entropy_record}])
                actor_net_params, critic_net_params = net_params_queue.get()
                actor.set_network_params(actor_net_params)
                critic.set_network_params(critic_net_params)
                s_batch = [np.zeros((a3c.S_INFO, a3c.S_LEN))]
                a_batch = [np.zeros(a3c.A_DIM)]
                r_batch = []
                entropy_record = []
            else:
                s_batch.append(state)
                action_vec = np.zeros(a3c.A_DIM)
                action_vec[bit_rate] = 1
                a_batch.append(action_vec)


# Main Training Loop
def main():
    np.random.seed(a3c.RANDOM_SEED)
    if not os.path.exists(SUMMARY_DIR):
        os.makedirs(SUMMARY_DIR)

    net_params_queues = []
    exp_queues = []
    for i in range(NUM_AGENTS):
        net_params_queues.append(mp.Queue(1))
        exp_queues.append(mp.Queue(1))

    coordinator = mp.Process(target=central_agent, args=(net_params_queues, exp_queues))
    coordinator.start()

    all_cooked_time, all_cooked_bw, _ = load_trace.load_trace(TRAIN_TRACES)
    agents = []
    for i in range(NUM_AGENTS):
        agents.append(mp.Process(target=agent, args=(i, all_cooked_time, all_cooked_bw, net_params_queues[i], exp_queues[i])))
    for i in range(NUM_AGENTS):
        agents[i].start()

    coordinator.join()


if __name__ == '__main__':
    main()