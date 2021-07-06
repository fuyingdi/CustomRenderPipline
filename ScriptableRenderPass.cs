using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public abstract class ScriptableRenderPass
{
    Color m_clearColor = Color.black;
    ClearFlag m_ClearFlag = ClearFlag.None;

    public abstract void Execute(ScriptableRenderContext context, Camera camera);
}
