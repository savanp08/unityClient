import tensorflow as tf
from tensorflow.keras import layers, models, optimizers
import numpy as np
import torch
import onnx
import tf2onnx

def create_abr_dqn_model():
    bio_signals_input = layers.Input(shape=(None, 8), name='bio_signals_input')
    bio_signals_cnn = layers.Conv1D(filters=128, kernel_size=4, strides=1, activation='relu')(bio_signals_input)
    bio_signals_cnn = layers.Flatten()(bio_signals_cnn)
    buffer_bw_input = layers.Input(shape=(None, 2), name='buffer_bw_input')
    buffer_bw_cnn = layers.Conv1D(filters=128, kernel_size=4, strides=1, activation='relu')(buffer_bw_input)
    buffer_bw_cnn = layers.Flatten()(buffer_bw_cnn)
    combined = layers.Concatenate()([bio_signals_cnn, buffer_bw_cnn])
    hidden_layer = layers.Dense(128, activation='relu')(combined)
    output_layer = layers.Dense(6, activation='softmax', name='output_layer')(hidden_layer)
    model = models.Model(inputs=[bio_signals_input, buffer_bw_input], outputs=output_layer)
    model.compile(optimizer=optimizers.Adam(learning_rate=0.001), loss='mse')
    return model

abr_dqn_model = create_abr_dqn_model()
abr_dqn_model.summary()

batch_size = 32
time_steps = 10

bio_signals_data = np.random.random((batch_size, time_steps, 8)).astype(np.float32)
buffer_bw_data = np.random.random((batch_size, time_steps, 2)).astype(np.float32)
target_q_values = np.random.random((batch_size, 6)).astype(np.float32)

abr_dqn_model.fit(
    [bio_signals_data, buffer_bw_data],
    target_q_values,
    epochs=10,
    batch_size=16
)

abr_dqn_model.save("abr_dqn_model")

import tf2onnx

def convert_to_onnx(model_dir, onnx_filename):
    model = tf.keras.models.load_model(model_dir)
    spec = (tf.TensorSpec((None, None, 8), tf.float32, name="bio_signals_input"),
            tf.TensorSpec((None, None, 2), tf.float32, name="buffer_bw_input"))
    output_path = onnx_filename
    model_proto, _ = tf2onnx.convert.from_keras(model, input_signature=spec, output_path=output_path)
    print(f"Model successfully converted to ONNX format and saved to {onnx_filename}")

convert_to_onnx("abr_dqn_model", "abr_dqn_model.onnx")

import onnxruntime

def test_onnx_model(onnx_filename, bio_signals_data, buffer_bw_data):
    session = onnxruntime.InferenceSession(onnx_filename)
    input_name_1 = session.get_inputs()[0].name
    input_name_2 = session.get_inputs()[1].name
    result = session.run(None, {input_name_1: bio_signals_data, input_name_2: buffer_bw_data})
    print("ONNX Model Output:")
    print(result)

sample_bio_signals_data = np.random.random((1, time_steps, 8)).astype(np.float32)
sample_buffer_bw_data = np.random.random((1, time_steps, 2)).astype(np.float32)

test_onnx_model("abr_dqn_model.onnx", sample_bio_signals_data, sample_buffer_bw_data)
