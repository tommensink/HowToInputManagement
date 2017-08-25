using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;



public static class ExtensionMethods
{
    // Recursive find methods
    //=======================
    public static Transform FindInChildren(this Transform self, string name)
    {
        // Recursive search
        int count = self.childCount;
        for (int i = 0; i < count; i++)
        {
            Transform child = self.GetChild(i);
            if (child.name == name) return child;
            Transform subChild = child.FindInChildren(name);
            if (subChild != null) return subChild;
        }
        return null;
    }

    public static GameObject FindInChildren(this GameObject self, string name)
    {
        Transform transform = self.transform;
        Transform child = transform.FindInChildren(name);
        return child != null ? child.gameObject : null;
    }


    // Set value in all childs
    // =======================
    public static void SetTexts(this GameObject gameObj, string text)
    {
        // Set name of the object in all 2D and 3D text elements
        foreach (Text child in gameObj.GetComponentsInChildren<Text>()) // GameObject recursive call, including this gameobject
        {
            child.text = text;
        }
        foreach (TextMesh child in gameObj.GetComponentsInChildren<TextMesh>()) // GameObject recursive call, including this gameobject
        {
            child.text = text;
        }
    }

    public static void SetActiveMeshRenderers(this GameObject gameObj, bool isActive)
    {
        foreach (MeshRenderer child in gameObj.GetComponentsInChildren<MeshRenderer>()) // GameObject recursive call, including this gameobject
        {
            child.enabled = isActive;
        }
    }

    public static void SetActiveRenderers(this GameObject gameObj, bool isActive)
    {
        foreach (Renderer child in gameObj.GetComponentsInChildren<Renderer>()) // GameObject recursive call, including this gameobject
        {
            child.enabled = isActive;
        }
    }

    // Get closest (need some work to make uniform) 
    // ============
    public enum SelectMethodEnum
    {
        Closest,
        BestView
    }

    public static GameObject GetClosestObject(GameObject[] objects, Vector3 currentPosition)
    {
        // Get 1 closest characters(to referencePos) - with Linq
        var bestTarget = objects.OrderBy(t => (t.transform.position - currentPosition).sqrMagnitude)
                           .FirstOrDefault();   //or use "Take(3), .ToArray();" if you need more
        return bestTarget;
    }

    public static int GetClosestPosition(Vector3[] targets, Vector3 currentPosition)
    {
        int bestTarget = 0;
        float closestDistanceSqr = Mathf.Infinity;
        for (int i = 0; i < targets.Length; i++)
        {
            Vector3 dirVect = targets[i] - currentPosition;
            float dSqrToTarget = dirVect.sqrMagnitude;
            if (dSqrToTarget < closestDistanceSqr)
            {
                closestDistanceSqr = dSqrToTarget;
                bestTarget = i;
            }
        }
        return bestTarget;
    }

    public static int GetClosestPosition(StoreTransform[] targets, Vector3 currentPosition, SelectMethodEnum selectMethod = SelectMethodEnum.BestView)
    {
        int bestTarget = 0;
        float closestDistance = Mathf.Infinity;
        for (int i = 0; i < targets.Length; i++)
        {
            // Calculate the best point
            Vector3 dirVect = targets[i].position - currentPosition;
            float distance = 0f;
            switch (selectMethod)
            {
                case SelectMethodEnum.Closest:
                    distance = dirVect.sqrMagnitude;
                    break;
                case SelectMethodEnum.BestView:
                    //distance = Vector3.Dot(dirVect.normalized, (targets[i].rotation * Vector3.forward).normalized);
                    distance = Vector3.Angle(dirVect, targets[i].rotation * Vector3.forward);
                    break;
            }
            if (distance < closestDistance)
            {
                closestDistance = distance;
                bestTarget = i;
            }
        }
        return bestTarget;
    }

    // Math extensions
    // ===============
    public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0)
            return min;
        if (value.CompareTo(max) > 0)
            return max;

        return value;
    }

    /*
    public static int Clamp(int value, int min, int max)
    {
        return Math.Min(max, Math.Max(value, min));
    }

    public static float Clamp(float value, float min, float max)
    {
        return Math.Min(max, Math.Max(value, min));
    }
    */

    // Bounding box calculators
    //========================
    // we return a transform (via a gameobject) and a size. The scale of the transform is not used, since it is bad practise to use this for size
    public static void GetObjectBounds(GameObject objectToBound, GameObject objectBounds, out Vector3 size, bool isWorld = false)
    {
        // Disable the gameobjects with trail renderers on them (here only subobjects), the result is not correct
        foreach (TrailRenderer render in objectToBound.GetComponentsInChildren<TrailRenderer>()) { render.gameObject.SetActive(false); }
        // TODO *****: We should remember the current state and restore that later, instead of enabling all

        Quaternion storeRotation = new Quaternion();

        // Establish a default empty bound
        Bounds occupiedSpace = new Bounds(Vector3.zero, Vector3.zero);

        if (!isWorld)
        {
            // Store our current rotation
            storeRotation = objectToBound.transform.rotation;

            // Zero the rotation before we calculate, as Unity bounds are never rotated
            objectToBound.transform.rotation = Quaternion.identity;
        }

        //Establish a default center location
        Vector3 center = Vector3.zero;

        // Count the children with renderers
        // We only count children with renderers, which should be fine as the bounds center is global space
        int count = 0;
        foreach (Renderer render in objectToBound.GetComponentsInChildren<Renderer>())
        {
            center += render.bounds.center;
            count++;
        }
        // Return the average center assuming we have any renderers
        if (count > 0)
            center /= count;
        
        // Update the parent bound accordingly
        occupiedSpace.center = center;
        
        // Again for each and only after updating the center expand via encapsulate
        foreach (Renderer render in objectToBound.GetComponentsInChildren<Renderer>())
        {
            if (render.GetType() != typeof(TrailRenderer)) 
            {
                occupiedSpace.Encapsulate(render.bounds);
            }
        }

        if (!isWorld)
        {
            //Restore our original rotation
            objectToBound.transform.rotation = storeRotation;
        }

        if (!isWorld)
        {
            // Calculate the bounding transform. correct for possible different centers
            Vector3 center2pivotVectW = occupiedSpace.center - objectToBound.transform.position;    // World coordinates
            Vector3 center2pivotVectR = center2pivotVectW;                                          // Local to gameobject coordinates. Was unity system, so the same
            Vector3 corrVect = objectToBound.transform.TransformDirection(center2pivotVectR);       // Local to World
            objectBounds.transform.position = objectToBound.transform.position + corrVect;
            objectBounds.transform.rotation = objectToBound.transform.rotation;
        }
        else
        {
            objectBounds.transform.position = occupiedSpace.center;
            objectBounds.transform.rotation = Quaternion.identity;
        }
        size = occupiedSpace.size;

        // Enable the trail renderers again
        foreach (TrailRenderer render in objectToBound.GetComponentsInChildren<TrailRenderer>(true)) { render.gameObject.SetActive(true); } // Do include the inactive components
        // TODO *****: We should remember the current state and restore that later, instead of enabling all
    }

    public static Vector3 GetObjectCenterPosition(this GameObject gameObj, bool isWorld = false)
    {
        GameObject boundsGameObj = new GameObject();
        Vector3 size = new Vector3();
        GetObjectBounds(gameObj, boundsGameObj, out size, isWorld);
        Vector3 centerPos = boundsGameObj.transform.position;
        GameObject.Destroy(boundsGameObj);
        return centerPos;

        /*
        Renderer renderer = gameObj.transform.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds.center;
        }
        else
        {
            return gameObj.transform.position;
        }
        */
    }

    // Extension method
    public static void SetObjectBounds(this GameObject objectBounds, GameObject objectToBound, bool isWorld = false)
    {
        Vector3 size = new Vector3();
        GetObjectBounds(objectToBound, objectBounds, out size, isWorld);
        objectBounds.transform.localScale = size;
    }

    // Extension method
    public static void SetObjectBounds(this SizedGameObject objectBounds, GameObject objectToBound, bool isWorld = false)
    {
        GetObjectBounds(objectToBound, objectBounds.gameObject, out objectBounds.Size, isWorld);
    }


    // Copy components
    //=======================
    // http://answers.unity3d.com/questions/530178/how-to-get-a-component-from-an-object-and-add-it-t.html?page=1&pageSize=5&sort=votes
    // Usage:
    //      var copy = myComp.GetCopyOf(someOtherComponent);
    /* build errors
    public static T GetCopyOf<T>(this MonoBehaviour comp, T source) where T : MonoBehaviour
    {
        //Type check
        Type type = comp.GetType();
        if (type != source.GetType()) return null;

        //Declare Binding Flags
        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;

        //Iterate through all types until monobehaviour is reached
        while (type != typeof(MonoBehaviour))
        {
            //Apply Fields
            FieldInfo[] fields = type.GetFields(flags);
            foreach (FieldInfo field in fields)
            {
                field.SetValue(comp, field.GetValue(source));
            }

            //Move to base class
            type = type.BaseType;
        }
        return comp as T;
    }
    */

    public static T GetCopyOf<T>(this Component comp, T source) where T : Component
    {
        //Type check
        Type type = comp.GetType();
        if (type != source.GetType()) return null; // type mis-match

        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | /* BindingFlags.Default |*/ BindingFlags.DeclaredOnly;

        PropertyInfo[] pinfos = type.GetProperties(flags);
        foreach (var pinfo in pinfos)
        {
            if (pinfo.CanWrite)
            {
                try
                {
                    pinfo.SetValue(comp, pinfo.GetValue(source, null), null);
                }
                catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
            }
        }

        //Apply Fields
        FieldInfo[] fields = type.GetFields(flags);
        foreach (var field in fields)
        {
            field.SetValue(comp, field.GetValue(source));
        }
        return comp as T;
    }

    // Extension method. Usage:
    //      Health myHealth = gameObject.AddComponent<Health>(enemy.health);
    public static T AddComponent<T>(this GameObject go, T toAdd) where T : MonoBehaviour
    {
        return go.AddComponent<T>().GetCopyOf(toAdd) as T;
    }

    /*
    // I don't think setting properties via reflection is the way to go, we should be setting the backing fields of the properties themselves, maybe it's just me but this seems cleaner. With the vexes method we can only access private fields of the type explicitly declared, as well as any public fields (If we remove BindingFlags.DeclaredOnly).
    // We want to access inherited private fields, and to do this, we need to check each type explicitly.
    public static T GetCopyOf<T>(this Component comp, T other) where T : Component
    {
        Type type = comp.GetType();
        if (type != other.GetType()) return null; // type mis-match
        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
        PropertyInfo[] pinfos = type.GetProperties(flags);
        foreach (var pinfo in pinfos)
        {
            if (pinfo.CanWrite)
            {
                try
                {
                    pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                }
                catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
            }
        }
        FieldInfo[] finfos = type.GetFields(flags);
        foreach (var finfo in finfos)
        {
            finfo.SetValue(comp, finfo.GetValue(other));
        }
        return comp as T;
    }
    */

    // Store and load Transforms
    //==========================
    public class StoreTransform
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;
        public Vector3 size; // For SizedGameObjects

        public StoreTransform()
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            localScale = Vector3.one;
            size = Vector3.zero; ;
        }
    }

    // http://answers.unity3d.com/questions/224774/copy-transform-at-runtime.html
    // Use "thïs" modifier to create extension methods. These can be called as if they are methods on (in this case) the Transform class. Local example:
    //      Transform someTransform;
    //      var save = someTransform.SaveWorld();
    //      [...]
    //      someTransform.LoadWorld(save);
    //
    // For global scope structs, you don't need a new call as for class instantiation:
    //      StoreTransform save;
    //      [...]
    //      save = someTransform.SaveWorld();
    //      [...]
    //      someTransform.LoadWorld(save);
    public static StoreTransform SaveLocal(this Transform aTransform)
    {
        StoreTransform storeTransform = new StoreTransform();
        storeTransform.position = aTransform.localPosition;
        storeTransform.rotation = aTransform.localRotation;
        storeTransform.localScale = aTransform.localScale;
        return storeTransform;
    }

    public static StoreTransform SaveWorld(this Transform aTransform)
    {
        StoreTransform storeTransform = new StoreTransform();
        storeTransform.position = aTransform.position;
        storeTransform.rotation = aTransform.rotation;
        storeTransform.localScale = aTransform.localScale; // LossyScale would be more accurate, however, this cannot be loaded back
        return storeTransform;
    }

    public static void LoadLocal(this Transform aTransform, StoreTransform aData)
    {
        aTransform.localPosition = aData.position;
        aTransform.localRotation = aData.rotation;
        aTransform.localScale = aData.localScale;
    }

    public static void LoadWorld(this Transform aTransform, StoreTransform aData)
    {
        aTransform.position = aData.position;
        aTransform.rotation = aData.rotation;
        aTransform.localScale = aData.localScale;
    }

    // Same extensions, but with SizedGameObject
    public static StoreTransform SaveLocal(this SizedGameObject aTransform)
    {
        StoreTransform storeTransform = new StoreTransform();
        storeTransform.position = aTransform.gameObject.transform.localPosition;
        storeTransform.rotation = aTransform.gameObject.transform.localRotation;
        storeTransform.localScale = aTransform.gameObject.transform.localScale;
        storeTransform.size = aTransform.Size;
        return storeTransform;
    }

    public static StoreTransform SaveWorld(this SizedGameObject aTransform)
    {
        StoreTransform storeTransform = new StoreTransform();
        storeTransform.position = aTransform.gameObject.transform.position;
        storeTransform.rotation = aTransform.gameObject.transform.rotation;
        storeTransform.localScale = aTransform.gameObject.transform.localScale; // LossyScale would be more accurate, however, this cannot be loaded back
        storeTransform.size = aTransform.Size;
        return storeTransform;
    }

    public static void LoadLocal(this SizedGameObject aTransform, StoreTransform aData)
    {
        aTransform.gameObject.transform.localPosition = aData.position;
        aTransform.gameObject.transform.localRotation = aData.rotation;
        aTransform.gameObject.transform.localScale = aData.localScale;
        aTransform.Size = aData.size;
    }

    public static void LoadWorld(this SizedGameObject aTransform, StoreTransform aData)
    {
        aTransform.gameObject.transform.position = aData.position;
        aTransform.gameObject.transform.rotation = aData.rotation;
        aTransform.gameObject.transform.localScale = aData.localScale;
        aTransform.Size = aData.size;
    }

    // Extension for Interpolator
    public static void SetTargetTransform(this HoloToolkit.Unity.Interpolator interpolator, StoreTransform aData)
    {
        interpolator.SetTargetPosition(aData.position);
        interpolator.SetTargetRotation(aData.rotation);
        interpolator.SetTargetLocalScale(aData.localScale);
    }
}


