using System;

namespace UnityXrefMaps.Tests
{
    public class XRefHrefFixerTests
    {
        [Theory]
        [InlineData(
            "https://docs.unity3d.com/6000.0/Documentation/ScriptReference/",
            "UnityEngine",
            "N:UnityEngine",
            "https://docs.unity3d.com/6000.0/Documentation/ScriptReference/index.html")]
        [InlineData(
            "https://docs.unity3d.com/6000.0/Documentation/ScriptReference/",
            "GameObject",
            "T:UnityEngine.GameObject",
            "https://docs.unity3d.com/6000.0/Documentation/ScriptReference/GameObject.html")]
        public void Unity_Fix(string apiUrl, string name, string commentId, string expected)
        {
            XrefMapReference xrefMapReference = new()
            {
                Name = name,
                CommentId = commentId,
            };

            Assert.Equal(expected, XRefHrefFixer.Fix(apiUrl, xrefMapReference, ["UnityEngine"], false));
        }

        [Theory]
        [InlineData(
            "https://docs.unity3d.com/Packages/com.unity.inputsystem@1.17/api/",
            "UnityEngine.InputSystem",
            "N:UnityEngine.InputSystem",
            "https://docs.unity3d.com/Packages/com.unity.inputsystem@1.17/api/UnityEngine.InputSystem.html")]
        [InlineData(
            "https://docs.unity3d.com/Packages/com.unity.inputsystem@1.17/api/",
            "InputSystem",
            "T:UnityEngine.InputSystem.InputSystem",
            "https://docs.unity3d.com/Packages/com.unity.inputsystem@1.17/api/UnityEngine.InputSystem.InputSystem.html")]
        [InlineData(
            "https://docs.unity3d.com/Packages/com.unity.inputsystem@1.17/api/",
            "Enable()",
            "M:UnityEngine.InputSystem.InputActionAsset.Enable",
            "https://docs.unity3d.com/Packages/com.unity.inputsystem@1.17/api/UnityEngine.InputSystem.InputActionAsset.html#UnityEngine_InputSystem_InputActionAsset_Enable")]
        [InlineData(
            "https://docs.unity3d.com/Packages/com.unity.inputsystem@1.17/api/",
            "Contains(InputAction)",
            "M:UnityEngine.InputSystem.InputActionAsset.Contains(UnityEngine.InputSystem.InputAction)",
            "https://docs.unity3d.com/Packages/com.unity.inputsystem@1.17/api/UnityEngine.InputSystem.InputActionAsset.html#UnityEngine_InputSystem_InputActionAsset_Contains_UnityEngine_InputSystem_InputAction_")]
        [InlineData(
            "https://docs.unity3d.com/Packages/com.unity.inputsystem@1.17/api/",
            "FindBinding(InputBinding, out InputAction)",
            "M:UnityEngine.InputSystem.InputActionAsset.FindBinding(UnityEngine.InputSystem.InputBinding,UnityEngine.InputSystem.InputAction@)",
            "https://docs.unity3d.com/Packages/com.unity.inputsystem@1.17/api/UnityEngine.InputSystem.InputActionAsset.html#UnityEngine_InputSystem_InputActionAsset_FindBinding_UnityEngine_InputSystem_InputBinding_UnityEngine_InputSystem_InputAction__")]
        [InlineData(
            "https://docs.unity3d.com/Packages/com.unity.inputsystem@1.17/api/",
            "AddDevice<TDevice>(string)",
            "M:UnityEngine.InputSystem.InputSystem.AddDevice``1(System.String)",
            "https://docs.unity3d.com/Packages/com.unity.inputsystem@1.17/api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_AddDevice__1_System_String_")]
        [InlineData(
            "https://docs.unity3d.com/Packages/com.unity.inputsystem@1.17/api/",
            "remoting",
            "P:UnityEngine.InputSystem.InputSystem.remoting",
            "https://docs.unity3d.com/Packages/com.unity.inputsystem@1.17/api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_remoting")]
        public void Package_Fix(string apiUrl, string name, string commentId, string expected)
        {
            XrefMapReference xrefMapReference = new()
            {
                Name = name,
                CommentId = commentId,
            };

            Assert.Equal(expected, XRefHrefFixer.Fix(apiUrl, xrefMapReference, Array.Empty<string>(), true));
        }
    }
}
