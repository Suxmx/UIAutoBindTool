using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using BindData = Autobind.ComponentAutoBindTool.BindData;

namespace Autobind.Editor
{
    /// <summary>
    /// 实体与界面代码生成器
    /// </summary>
    public class AutobindToolConfigWindow : EditorWindow
    {
        public enum GenCodeType
        {
            UIForm,
            UIItem
        }

        private const string UIItemBaseCodePath = "Assets/GameMain/Scripts/UI/UIItems";
        private const string UIFormBaseCodePath = "Assets/GameMain/Scripts/UI/UIForms";
        private const string DefaultNameSpace = "GameMain";
        private string UIFormCodePath;
        private string UIItemCodePath;
        private string UINamespace;


        [MenuItem("Tools/UI自动绑定路径设置")]
        public static void OpenCodeGeneratorWindow()
        {
            AutobindToolConfigWindow window =
                GetWindowWithRect<AutobindToolConfigWindow>(new Rect(500, 500, 500, 150), true);
            window.minSize = new Vector2(500, 100);
            window.titleContent = new GUIContent("UI自动绑定路径设置");
        }

        private void OnEnable()
        {
            UIFormCodePath = EditorPrefs.GetString(Application.productName + "_UIFormAutobindPath");
            UIItemCodePath = EditorPrefs.GetString(Application.productName + "_UIItemAutobindPath");
            UINamespace = EditorPrefs.GetString(Application.productName + "_UIItemAutobindNamespace");

            UIFormCodePath = string.IsNullOrEmpty(UIFormCodePath) ? UIFormBaseCodePath : UIFormCodePath;
            UIItemCodePath = string.IsNullOrEmpty(UIItemCodePath) ? UIItemBaseCodePath : UIItemCodePath;
            UINamespace = string.IsNullOrEmpty(UINamespace) ? DefaultNameSpace : UINamespace;
        }

        private void OnGUI()
        {
            //绘制自动生成代码类型的弹窗
            EditorGUILayout.LabelField("UIForm");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("自动生成的代码类型：", GUILayout.Width(140f));


            EditorGUILayout.LabelField(UIFormCodePath);
            if (GUILayout.Button("选择"))
            {
                ChooseFormAddress();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("UIItem");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("自动生成的代码路径：", GUILayout.Width(140f));
            EditorGUILayout.LabelField(UIItemCodePath);
            if (GUILayout.Button("选择"))
            {
                ChooseItemAddress();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("命名空间：", GUILayout.Width(140f));
            EditorGUI.BeginChangeCheck();
            UINamespace = EditorGUILayout.TextField(UINamespace);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString(Application.productName + "_UIItemAutobindNamespace", UINamespace);
            }

            EditorGUILayout.EndHorizontal();
        }


        #region UIForm

        private void ChooseFormAddress()
        {
            string defaultPath = $"Assets/";
            string filePath = EditorUtility.OpenFolderPanel("选择UIForm代码存储位置", defaultPath, "");
            if (filePath.Contains("Assets/"))
            {
                int index = filePath.IndexOf("Assets/", StringComparison.Ordinal);
                filePath = filePath.Substring(index);
            }

            if (!string.IsNullOrEmpty(filePath))
            {
                EditorPrefs.SetString(Application.productName + "_UIFormAutobindPath", filePath);
                UIFormCodePath = filePath;
            }
        }

        #endregion

        #region UIItem

        private void ChooseItemAddress()
        {
            string defaultPath = $"Assets/";
            string filePath = EditorUtility.OpenFolderPanel("选择UIItem代码存储位置", defaultPath, "");
            if (filePath.Contains("Assets/"))
            {
                int index = filePath.IndexOf("Assets/", StringComparison.Ordinal);
                filePath = filePath.Substring(index);
            }

            if (!string.IsNullOrEmpty(filePath))
            {
                EditorPrefs.SetString(Application.productName + "_UIItemAutobindPath", filePath);
                UIItemCodePath = filePath;
            }
        }

        #endregion
    }
}