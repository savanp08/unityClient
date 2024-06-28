using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.OpenXR.Input;

public class EyeGazeTest : MonoBehaviour
{
    [SerializeField] private InputActionAsset actionAsset;  // Your action asset
    private InputAction eyePoseAction;                      // Store the action itself

    private bool methodCalled = false;
    private void OnEnable()
    {
        // Find the action by name within the asset
        var actionMap = actionAsset.FindActionMap("EyeManager");
        if (actionMap != null)
        {
            eyePoseAction = actionMap.FindAction("EyeAction");
        }
        else
        {
            Debug.LogError("Action map 'EyeManager' not found in the asset!");
            return;
        }

        // Log actions in the asset
        Debug.Log($"--->>>>> TEST3: ---->>> Actions in asset: {actionAsset.actionMaps.Count}");
        foreach (var map in actionAsset.actionMaps)
        {
            Debug.Log($"--->>>>> TEST3: ---->>> Action map: {map.name}");
            foreach (var action in map.actions)
            {
                Debug.Log($"--->>>>> TEST3: ---->>> Action: {action.name}");
            }
        }

        // If the action was found, enable it
        if (eyePoseAction != null)
        {
            eyePoseAction.Enable();
        }
        else
        {
            Debug.LogError("Eye pose action not found in the action map 'EyeManager'!");
        }
    }
private void InitMethod()
    {
        Debug.Log("---->>>>> TEST3: ---->>> InitMethod called!");
        // Find the action by name within the asset
        var actionMap = actionAsset.FindActionMap("EyeManager");
        if (actionMap != null)
        {
            eyePoseAction = actionMap.FindAction("EyeAction");
            // list all actions in actionMap
            Debug.Log($"---->>>>> TEST3: ---->>> Actions in actionMap: {actionMap.actions.Count}");
            foreach (var action in actionMap.actions)
            {
                Debug.Log($"---->>>>> TEST3: ---->>> Action: {action.name}");
            }
        }
        else
        {
            Debug.LogError("Action map 'EyeManager' not found in the asset!");
            return;
        }

        // Log actions in the asset
        Debug.Log($"--->>>>> TEST3: ---->>> Actions in asset: {actionAsset.actionMaps.Count}");
        foreach (var map in actionAsset.actionMaps)
        {
            Debug.Log($"--->>>>> TEST3: ---->>> Action map: {map.name}");
            foreach (var action in map.actions)
            {
                Debug.Log($"--->>>>> TEST3: ---->>> Action: {action.name}");
            }
        }

        // If the action was found, enable it
        if (eyePoseAction != null)
        {
            eyePoseAction.Enable();
        }
        else
        {
            Debug.LogError("Eye pose action not found in the action map 'EyeManager'!");
        }
    }
    private void OnDisable()
    {
        if (eyePoseAction != null)
        {
            eyePoseAction.Disable();
        }
        else
        {
            Debug.LogError("---->>>>> TEST3: ---->>> Eye pose action not found in the action map 'EyeManager'!");
        }
    }

    private void Update()
    {
        if(methodCalled == false)
        {
            InitMethod();
            methodCalled = true;
        }
        if (eyePoseAction != null && eyePoseAction.phase == InputActionPhase.Performed)
        {
            UnityEngine.XR.OpenXR.Input.Pose pose = eyePoseAction.ReadValue<UnityEngine.XR.OpenXR.Input.Pose>();
            Debug.Log($"---->>>>> Test3 ---->>> Position: {pose.position} Rotation: {pose.rotation}");
        }
        else
        {
            Debug.Log("---->>>>> Test3 ---->>> No eye pose action found or action not performed!");
        }
    }
}
