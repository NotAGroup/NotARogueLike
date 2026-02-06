using UnityEngine;
using UnityEngine.Events;

// component to receive and handle UI navigation events
public class UINavigationReceiver : MonoBehaviour
{
    public UnityEvent<Vector2> navigationEvent;
    public UnityEvent submitEvent;
    public UnityEvent cancelEvent;

    public void Navigate(Vector2 direction) { navigationEvent.Invoke(direction); }
    public void Submit() { submitEvent.Invoke(); }
    public void Cancel() { cancelEvent.Invoke(); }
}
