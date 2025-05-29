using UnityEngine;

public class CameraMoved : MonoBehaviour
{
    public bool TransformChanged;

    // Update is called once per frame
    void Update()
    {
        TransformChanged = false;

        if (this.transform.hasChanged)
        {
            TransformChanged = true;
            this.transform.hasChanged = false;
        }
    }
}
