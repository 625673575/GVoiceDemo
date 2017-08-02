using UnityEngine.UI;

using System.Collections.Generic;
using UnityEngine;
public class Panel_UDP:MonoBehaviour{
    [ContextMenu("Connect")]
    private void ccc()
    {
        foreach(var f in this.GetType().GetFields())
        {
            if(f.FieldType.Namespace== "UnityEngine.UI")
            {
                f.SetValue(this, transform.Find(f.Name).GetComponent(f.FieldType));
            }
        }
    }
    public Button Button_Send;

public Button Button_Voice;

public Button Button_Connect;

}