using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This is a kind of billboard that switches between a number of discrete positions around a central point.
// The point that is closest to the camera is chosen.
// The gameobject will be positioned on the switch point, turning also (as with adapted Billboard!) its rotation
public class SwitchBoard : MonoBehaviour
{
    [Tooltip("Parent of this control, which acts as the pivot of the switch positions. Empty uses the current parent. The parent must contain a SizedGameObject")]
    public SizedGameObject ParentPivot;

    [Tooltip("Number of discrete turn points")]
    public int NrOfTurnPoints = 4;

    [Tooltip("Relative offset of first position from the pivot center (0.5 is the edge)")]
    public Vector3 percOffset = new Vector3(0f, -0.5f, -0.5f);

    [Tooltip("Absolute offset of first position from the pivot center")]
    public Vector3 absOffset = new Vector3(0f, 0f, 0f);

    [Tooltip("How to select the turn point, on closest position or smallest view angle")]
    public ExtensionMethods.SelectMethodEnum SelectMethod = ExtensionMethods.SelectMethodEnum.BestView;

    // Transforms holders
    protected ExtensionMethods.StoreTransform _startTransform;
    protected ExtensionMethods.StoreTransform[] _turnTransforms;
    protected int _currentTransformIndex = 0;

    // Event management
    private bool _FirstTimeDone = false;

    protected virtual void Awake()
    {
        // If no parent set, then try if your own parent is a SizedGameObject
        if (ParentPivot == null)
        {
            ParentPivot = gameObject.transform.parent.GetComponentInChildren<SizedGameObject>();
        }
        if (ParentPivot == null)
        {
            Debug.LogError("Missing inspector property ParentPivot or the parent does not contain a SizedGameObject component", gameObject);
            Destroy(gameObject);
            return;
        }
        if (NrOfTurnPoints < 1)
        {
            Debug.LogError("Nr Of Turn Points must be larger than 0");
            Destroy(gameObject);
            return;
        }
    }

    protected virtual void Start()
    {
        // Holder for the transforms
        _turnTransforms = new ExtensionMethods.StoreTransform[NrOfTurnPoints];
    }

    protected virtual void Update()
    {
        // First time init
        if (!_FirstTimeDone)
        {
            CalculateRelativePosition();
            CalculateTurnTransforms();
            _FirstTimeDone = true;
        }

        ChooseTurnPosition();
    }

    public virtual void CalculateRelativePosition()
    {
        // Store current relative transform of this object, as a base for positioning
        _startTransform = gameObject.transform.SaveLocal();
    }

    public virtual void CalculateTurnTransforms()
    {
        // Calculate the switch transforms
        float angleIncrement = 360.0f / NrOfTurnPoints;
        for (int i = 0; i < NrOfTurnPoints; i++)
        {
            // Start with index 0 position
            // Set rotation
            gameObject.transform.LoadLocal(_startTransform); 
            // Set position on the pivot point
            gameObject.transform.position = ParentPivot.transform.position;
            // Repositon in ratio with the parent size
            gameObject.transform.localPosition += Vector3.Scale(percOffset, ParentPivot.Size);
            // Add offset
            gameObject.transform.localPosition += absOffset;

            // Determine centerpos and rotation axis
            Vector3 centerPos = ParentPivot.transform.position;
            Vector3 yAxis = ParentPivot.transform.up;

            // Rotate
            gameObject.transform.RotateAround(centerPos, yAxis, i * angleIncrement); // Note: RotateAround is in world coordinates 

            // Store
            _turnTransforms[i] = gameObject.transform.SaveWorld();
        }

        // Position the objects on the edge of the MenuObjectSizeObject cube by incorporating the size of the Pivot object
        // - X is the switch point as calculated by rotation 
        // - Y is the point on the edge of the pivot object. Assuming it is elliptical of shape in the X/Z direction. (Y is vertical)
        // - Xsized is the switch point located on the edge, but corrected for the same offsets as first position: X[0]-Y[0]
        Vector3 Scale = new Vector3(ParentPivot.Size.x, 0, ParentPivot.Size.z) * 0.5f;
        float extra = 0f;
        for (int i = 0; i < NrOfTurnPoints; i++)
        {
            Vector3 X_W = _turnTransforms[i].position;
            Vector3 X_P = ParentPivot.transform.InverseTransformPoint(X_W);
            float X_P_y = X_P.y;
            X_P.y = 0f;
            Vector3 X_P_dir = X_P.normalized;
            Vector3 Y_P = Vector3.Scale(X_P_dir, Scale);
            if (i == 0) {
                extra = (X_P - Y_P).magnitude;
            }
            else
            {
                Vector3 Xsized_P = Y_P + X_P_dir * extra;
                Xsized_P.y = X_P_y;
                Vector3 Xsized_W = ParentPivot.transform.TransformPoint(Xsized_P);
                _turnTransforms[i].position = Xsized_W;
            }
        }

        // Restore the current position
        SwichToTurnPosition(_currentTransformIndex, true);
    }

    public virtual void SwichToTurnPosition(int index, bool force = false)
    {
        if (index != _currentTransformIndex || force)
        {
            gameObject.transform.LoadWorld(_turnTransforms[index]);
            _currentTransformIndex = index;
        }
    }

    public virtual void ChooseTurnPosition()
    {
        // Use the current camera position
        int index = ExtensionMethods.GetClosestPosition(_turnTransforms, Camera.main.transform.position, SelectMethod);

        // Switch to this point
        SwichToTurnPosition(index);
    }
}
