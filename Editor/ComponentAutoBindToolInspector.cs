using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ComponentAutoBindTool = Autobind.ComponentAutoBindTool;

namespace Autobind.Editor
{
    [CustomEditor(typeof(ComponentAutoBindTool))]
    public class ComponentAutoBindToolInspector : UnityEditor.Editor
    {
        private bool expandRules = false;

        private bool ExpandRules
        {
            get => expandRules;
            set
            {
                if (expandRules != value)
                {
                    EditorPrefs.SetBool("COM_AUTO_BIND_TOOL_EXPAND", value);
                    expandRules = value;
                }
            }
        }

        private SerializedProperty m_BindDatas;
        private SerializedProperty m_BindComs;
        private List<ComponentAutoBindTool.BindData> m_TempList = new List<ComponentAutoBindTool.BindData>();
        private List<string> m_TempFiledNames = new List<string>();
        private List<string> m_TempComponentTypeNames = new List<string>();

        private SearchField m_SearchField;
        private string m_SearchStr = string.Empty;
        static int m_ComponentFilterFlags = -1; //-1表示All
        private string[] m_ComponentFilterOptions;


        //各种类型的代码生成后的路径
        private const string UIItemBaseCodePath = "Assets/GameMain/Scripts/UI/UIItems";
        private const string UIFormBaseCodePath = "Assets/GameMain/Scripts/UI/UIForms";
        private const string DefaultNameSpace = "GameMain";
        private string UIFormCodePath;
        private string UIItemCodePath;
        private string UINamespace;

        public static Dictionary<Type, string> ComponentPrefixName = new Dictionary<Type, string>()
        {
            { typeof(SpriteRenderer), "m_sprite" },
            { typeof(Transform), "m_trans" },
            { typeof(Animation), "m_animation" },
            { typeof(Animator), "m_animator" },
            { typeof(RectTransform), "m_rect" },
            { typeof(Canvas), "m_canvas" },
            { typeof(CanvasGroup), "m_group" },
            { typeof(VerticalLayoutGroup), "m_vgroup" },
            { typeof(HorizontalLayoutGroup), "m_hgroup" },
            { typeof(GridLayoutGroup), "m_ggroup" },
            { typeof(Button), "m_btn" },
            { typeof(Image), "m_img" },
            { typeof(RawImage), "m_rawimg" },
            { typeof(Text), "m_txt" },
            { typeof(InputField), "m_input" },
            { typeof(Slider), "m_slider" },
            { typeof(Mask), "m_mask" },
            { typeof(RectMask2D), "m_mask2d" },
            { typeof(Toggle), "m_tog" },
            { typeof(Scrollbar), "m_scrollbar" },
            { typeof(ScrollRect), "m_scrollrect" },
            { typeof(Dropdown), "m_drop" },
            { typeof(EventTrigger), "m_event" },
            { typeof(ToggleGroup), "m_tgroup" },

            { typeof(Camera), "m_camera" },

            //{typeof(SlideTouchButton),"m_slideBtn"}
            // {typeof(RemoteImage),"m_remoteimg"}
        };

        /// <summary>
        /// 命名前缀与类型的映射
        /// </summary>
        private Dictionary<string, string> m_PrefixesDict = new Dictionary<string, string>()
        {
            { "m_sprite", "SpriteRenderer" },
            { "m_trans", "Transform" },
            { "m_animation", "Animation" },
            { "m_animator", "Animator" },

            { "m_rect", "RectTransform" },
            { "m_canvas", "Canvas" },
            { "m_group", "CanvasGroup" },
            { "m_vgroup", "VerticalLayoutGroup" },
            { "m_hgroup", "HorizontalLayoutGroup" },
            { "m_ggroup", "GridLayoutGroup" },

            { "m_btn", "Button" },
            { "m_uiBtn", "UIButton" },
            { "m_img", "Image" },
            { "m_rawimg", "RawImage" },
            { "m_txt", "Text" },
            { "m_tmp_", "TextMeshProUGUI" },

            { "m_input", "InputField" },
            { "m_selfinput", "SelfInputField" },
            { "m_slider", "Slider" },
            { "m_mask", "Mask" },
            { "m_mask2d", "RectMask2D" },
            { "m_tog", "Toggle" },
            { "m_scrollbar", "Scrollbar" },
            { "m_scrollrect", "ScrollRect" },
            { "m_drop", "Dropdown" },
            { "m_Progress", "ProgressBar" },
        };

        private void OnEnable()
        {
            m_BindDatas = serializedObject.FindProperty("BindDatas");
            m_BindComs = serializedObject.FindProperty("m_BindComs");
            expandRules = EditorPrefs.GetBool("COM_AUTO_BIND_TOOL_EXPAND", true);
            m_SearchField = new SearchField();
            m_ComponentFilterOptions = new string[m_PrefixesDict.Count];
            m_ComponentFilterFlags = -1;
            int index = 0;
            foreach (var item in m_PrefixesDict)
            {
                m_ComponentFilterOptions[index] = item.Value;
                index++;
            }
        }


        public override void OnInspectorGUI()
        {
            //绘制缩写规则
            if (GUILayout.Button("展开支持的绑定缩写规则"))
            {
                ExpandRules = !ExpandRules;
            }

            if (ExpandRules)
            {
                EditorGUILayout.BeginVertical();
                foreach (var item in m_PrefixesDict)
                {
                    GUILayout.Label(string.Format("{0}=>{1}", item.Key, item.Value));
                }

                EditorGUILayout.EndVertical();
            }

            //绘制功能按钮
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("排序"))
            {
                Sort();
                GenerateCode();
            }

            if (GUILayout.Button("全部删除"))
            {
                RemoveAll();
                GenerateCode();
            }

            if (GUILayout.Button("删除空引用"))
            {
                RemoveNull();
                GenerateCode();
            }

            if (GUILayout.Button("自动绑定"))
            {
                AutoBindComponent();
                GenerateCode();
            }

            if (GUILayout.Button("路径设置"))
            {
                AutobindToolConfigWindow.OpenCodeGeneratorWindow();
            }

            // if (GUILayout.Button("自动绑定元组件"))
            // {
            //     if (EditorUtility.DisplayDialog("是否点击元组件？", "自动绑定元组件(元组件类似于Item但是轻量型直接摆放预制体，挂载Mono可直接引用修改。详情请看UIItemtransRockerOutPanel物体,注意后缀应为Panel)", "确定", "取消"))
            //     {
            //         if (EditorUtility.DisplayDialog("是否生成音效脚本？",  "是否生成音效脚本","确定", "取消"))
            //         {
            //             AutoBindComponent();
            //             GenerateItemCode(true);
            //         }
            //         else
            //         {
            //             AutoBindComponent();
            //             GenerateItemCode();
            //         }
            //     }
            // }


            EditorGUILayout.EndHorizontal();
            //搜素
            EditorGUILayout.BeginHorizontal();
            m_SearchStr = m_SearchField.OnGUI(GUILayoutUtility.GetRect(100, 16), m_SearchStr);
            m_ComponentFilterFlags =
                EditorGUILayout.MaskField(m_ComponentFilterFlags, m_ComponentFilterOptions, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
            DrawKvData();


            serializedObject.ApplyModifiedProperties();
            serializedObject.UpdateIfRequiredOrScript();
        }


        /// <summary>
        /// 排序
        /// </summary>
        private void Sort()
        {
            ComponentAutoBindTool target = (ComponentAutoBindTool)this.target;

            m_TempList.Clear();
            foreach (ComponentAutoBindTool.BindData data in target.BindDatas)
            {
                m_TempList.Add(new ComponentAutoBindTool.BindData(data.Name, data.BindCom));
            }

            m_TempList.Sort((x, y) => { return string.Compare(x.Name, y.Name, StringComparison.Ordinal); });

            m_BindDatas.ClearArray();
            foreach (ComponentAutoBindTool.BindData data in m_TempList)
            {
                AddBindData(data.Name, data.BindCom);
            }

            SyncBindComs();
        }

        /// <summary>
        /// 全部删除
        /// </summary>
        private void RemoveAll()
        {
            m_BindDatas.ClearArray();

            SyncBindComs();
        }

        /// <summary>
        /// 删除空引用
        /// </summary>
        private void RemoveNull()
        {
            for (int i = m_BindDatas.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty element = m_BindDatas.GetArrayElementAtIndex(i).FindPropertyRelative("Obj");
                if (element == null || element.objectReferenceValue == null)
                {
                    m_BindDatas.DeleteArrayElementAtIndex(i);
                }
            }

            SyncBindComs();
        }

        /// <summary>
        /// 自动绑定组件
        /// </summary>
        private void AutoBindComponent()
        {
            m_BindDatas.ClearArray();
            GameObject gameobject = ((ComponentAutoBindTool)target).gameObject;
            List<Transform> childs = new List<Transform>();
            GetChildsNotIncludBaseLife(gameobject.transform, childs);
            foreach (Transform child in childs)
            {
                m_TempFiledNames.Clear();
                m_TempComponentTypeNames.Clear();

                if (IsValidBindAllowUnderline(child, m_TempFiledNames, m_TempComponentTypeNames))
                {
                    for (int i = 0; i < m_TempFiledNames.Count; i++)
                    {
                        Component com = child.GetComponent(m_TempComponentTypeNames[i]);
                        if (com == null)
                        {
                            if (child.name.Contains("'"))
                            {
                                Debug.LogWarning($"{child.name}上不存在{m_TempComponentTypeNames[i]}的组件");
                            }
                            else
                            {
                                Debug.LogError($"{child.name}上不存在{m_TempComponentTypeNames[i]}的组件");
                            }
                        }
                        else
                        {
                            AddBindData(m_TempFiledNames[i], child.GetComponent(m_TempComponentTypeNames[i]));
                        }
                    }
                }
            }

            SyncBindComs();
        }

        void GetChildsNotIncludBaseLife(Transform transform, List<Transform> list)
        {
            if (transform.childCount == 0)
            {
                return;
            }

            if (transform.name.StartsWith("m_item"))
            {
                return;
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                Debug.Log($"auto bind transform name {child.name}");
                list.Add(child);
                // if (child.GetComponent<BasePanelLife>())
                // {
                //     Debug.Log($"auto bind transform name {child.name} continue ...");
                //     continue;
                // }
                // if (child.GetComponent<SnakeMain.HotfixUGuiItem>() && !child.name.StartsWith("m_item"))
                // {
                //     Debug.Log($"auto bind transform name {child.name} continue ...");
                //     continue;
                // }
                GetChildsNotIncludBaseLife(child, list);
            }
        }

        private void GenerateCode()
        {
            //获取路径
            UIFormCodePath = EditorPrefs.GetString(Application.productName + "_UIFormAutobindPath");
            UIItemCodePath = EditorPrefs.GetString(Application.productName + "_UIItemAutobindPath");
            UINamespace = EditorPrefs.GetString(Application.productName + "_UIItemAutobindNamespace");

            UIFormCodePath = string.IsNullOrEmpty(UIFormCodePath) ? UIFormBaseCodePath : UIFormCodePath;
            UIItemCodePath = string.IsNullOrEmpty(UIItemCodePath) ? UIItemBaseCodePath : UIItemCodePath;
            UINamespace = string.IsNullOrEmpty(UINamespace) ? DefaultNameSpace : UINamespace;

            var tool = (ComponentAutoBindTool)target;
            GameObject gameobject = tool.gameObject;
            List<ComponentAutoBindTool.BindData> datas = new List<ComponentAutoBindTool.BindData>();
            foreach (SerializedProperty item in m_BindDatas)
            {
                string name = item.FindPropertyRelative("Name").stringValue;
                Component com = item.FindPropertyRelative("BindCom").objectReferenceValue as Component;

                ComponentAutoBindTool.BindData data = new ComponentAutoBindTool.BindData(name, com);
                datas.Add(data);
            }

            GenerateCode(gameobject, datas, UIFormCodePath, UIItemCodePath, UINamespace);
        }


        /// <summary>
        /// 是否为有效绑定
        /// 有多个组件类型时，组件类型和 '_' 都必须放在组件名之前，组件类型之间顺序无要求
        /// IsValidBind方法的优化，除了组件类型外，组件名中也允许含有若干个 '_'
        /// </summary>
        private bool IsValidBindAllowUnderline(Transform target, List<string> filedNames,
            List<string> componentTypeNames)
        {
            string tname = target.name;
            if (!tname.StartsWith("m_"))
                return false;

            foreach (var rule in m_PrefixesDict)
            {
                if (tname.StartsWith(rule.Key))
                {
                    filedNames.Add(tname);
                    componentTypeNames.Add(rule.Value);
                    return true;
                }
            }

            Debug.LogError($"{target.name}的命名中不存在对应的组件类型，绑定失败");
            return false;
        }

        /// <summary>
        /// 是否为有效绑定
        /// 除了组件类型外，组件名中不可包含 '_'
        /// </summary>
        private bool IsValidBind(Transform target, List<string> filedNames, List<string> componentTypeNames)
        {
            string[] strArray = target.name.Split('_');

            if (strArray.Length == 1)
            {
                return false;
            }

            string filedName = strArray[strArray.Length - 1];

            for (int i = 0; i < strArray.Length - 1; i++)
            {
                string str = strArray[i];
                string comName;
                if (m_PrefixesDict.TryGetValue(str, out comName))
                {
                    filedNames.Add($"{str}_{filedName}");
                    componentTypeNames.Add(comName);
                }
                else
                {
                    Debug.LogError($"{target.name}的命名中{str}不存在对应的组件类型，绑定失败");
                    return false;
                }
            }

            return true;
        }

        private bool CheckDrawKvData(SerializedProperty sp)
        {
            SerializedProperty nameProperty = sp.FindPropertyRelative("Name");
            SerializedProperty comProperty = sp.FindPropertyRelative("BindCom");
            //名称搜索不通过
            if (string.IsNullOrEmpty(m_SearchStr) == false &&
                nameProperty.stringValue.ToLower().Contains(m_SearchStr.ToLower()) == false)
            {
                return false;
            }

            if (comProperty == null || comProperty.objectReferenceValue == null)
            {
                return true;
            }

            string comTypeName = comProperty.objectReferenceValue.GetType().Name;
            //类型检索通过
            for (int i = 0; i < m_ComponentFilterOptions.Length; i++)
            {
                if ((m_ComponentFilterFlags & 1 << i) != 0)
                {
                    if (m_ComponentFilterOptions[i] == comTypeName)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 绘制键值对数据
        /// </summary>
        private void DrawKvData()
        {
            //绘制key value数据

            int needDeleteIndex = -1;

            EditorGUILayout.BeginVertical();
            SerializedProperty property;
            for (int i = 0; i < m_BindDatas.arraySize; i++)
            {
                if (CheckDrawKvData(m_BindDatas.GetArrayElementAtIndex(i)) == false)
                {
                    continue;
                }

                EditorGUILayout.BeginHorizontal();
                property = m_BindDatas.GetArrayElementAtIndex(i).FindPropertyRelative("Name");
                property.stringValue = EditorGUILayout.TextField(property.stringValue, GUILayout.Width(150));
                property = m_BindDatas.GetArrayElementAtIndex(i).FindPropertyRelative("BindCom");
                property.objectReferenceValue =
                    EditorGUILayout.ObjectField(property.objectReferenceValue, typeof(Component), true);

                if (GUILayout.Button("X"))
                {
                    //将元素下标添加进删除list
                    needDeleteIndex = i;
                }

                EditorGUILayout.EndHorizontal();
            }

            //删除data
            if (needDeleteIndex != -1)
            {
                m_BindDatas.DeleteArrayElementAtIndex(needDeleteIndex);
                SyncBindComs();
                GenerateCode();
            }

            EditorGUILayout.EndVertical();
        }


        /// <summary>
        /// 添加绑定数据
        /// </summary>
        private void AddBindData(string name, Component bindCom)
        {
            int index = m_BindDatas.arraySize;
            m_BindDatas.InsertArrayElementAtIndex(index);
            SerializedProperty element = m_BindDatas.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("Name").stringValue = name;
            element.FindPropertyRelative("BindCom").objectReferenceValue = bindCom;
        }

        /// <summary>
        /// 同步组件数据
        /// </summary>
        private void SyncBindComs()
        {
            m_BindComs.ClearArray();

            SerializedProperty property;
            for (int i = 0; i < m_BindDatas.arraySize; i++)
            {
                property = m_BindDatas.GetArrayElementAtIndex(i).FindPropertyRelative("BindCom");
                m_BindComs.InsertArrayElementAtIndex(i);
                m_BindComs.GetArrayElementAtIndex(i).objectReferenceValue = property.objectReferenceValue;
            }

            serializedObject.ApplyModifiedProperties();
        }

        #region Generator

        public static void GenerateCode(GameObject go, List<ComponentAutoBindTool.BindData> bindDatas, string formPath,
            string itemPath, string _nameSpace)
        {
            if (go == null) return;
            AutobindToolConfigWindow.GenCodeType type = AutobindToolConfigWindow.GenCodeType.UIForm;
            if (go.name.EndsWith("Form"))
            {
                type = AutobindToolConfigWindow.GenCodeType.UIForm;
            }
            else if (go.name.EndsWith("Item"))
            {
                type = AutobindToolConfigWindow.GenCodeType.UIItem;
            }

            string codepath = string.Empty;
            if (type == AutobindToolConfigWindow.GenCodeType.UIForm)
            {
                string clsName = go.name;
                // UGuiForm ui = go.GetComponent<UGuiForm>();
                // if (ui != null)
                {
                    codepath = formPath;
                    string nameSpace = _nameSpace;
                    GenAutoBindCode(go, bindDatas, type, codepath, clsName, nameSpace);
                }
                // else
                // {
                //     EditorUtility.DisplayDialog("错误", "未找到挂载的UGuiForm组件", "OK");
                //     return;
                // }


                AssetDatabase.Refresh();
            }
            else if (type == AutobindToolConfigWindow.GenCodeType.UIItem)
            {
                codepath = itemPath;
                string clsName = go.name;
                // UGuiItem ui = go.GetComponent<UGuiItem>();
                // if (ui != null)
                {
                    string nameSpace = _nameSpace;
                    GenAutoItemBindCode(go, bindDatas, type, codepath, clsName, nameSpace);
                }
                // else
                // {
                //     Debug.LogError("未找到对应UGuiItem脚本!!!!");
                // }

                AssetDatabase.Refresh();
            }
        }


        private static void GenAutoBindCode(GameObject go, List<ComponentAutoBindTool.BindData> bindDatas,
            AutobindToolConfigWindow.GenCodeType type, string codePath,
            string clsName, string nameSpace, string nameEx = "")
        {
            ComponentAutoBindTool bindTool = go.GetComponent<ComponentAutoBindTool>();
            if (bindTool == null)
            {
                return;
            }

            if (!Directory.Exists($"{codePath}/BindComponents/"))
            {
                Directory.CreateDirectory($"{codePath}/BindComponents/");
            }

            string scriptPath = $"{codePath}/BindComponents/{clsName}{nameEx}.BindComponents.cs";

            using (StreamWriter sw = new StreamWriter(scriptPath))
            {
                HashSet<string> namespaceHashSet = new HashSet<string>();
                foreach (ComponentAutoBindTool.BindData data in bindDatas)
                {
                    if (!string.IsNullOrEmpty(data.BindCom.GetType().Namespace))
                    {
                        namespaceHashSet.Add(data.BindCom.GetType().Namespace);
                    }
                }

                namespaceHashSet.Add("UnityEngine");
                namespaceHashSet.Add("Autobind");

                var namespaceList = namespaceHashSet.ToList();
                namespaceList.Sort();
                for (int i = 0; i < namespaceList.Count; i++)
                {
                    sw.WriteLine($"using {namespaceList[i]};");
                }

                sw.WriteLine("");

                sw.WriteLine("//自动生成于：" + DateTime.Now);

                //命名空间

                sw.WriteLine("namespace " + nameSpace);


                sw.WriteLine("{");
                sw.WriteLine("");

                //类名
                sw.WriteLine($"\tpublic partial class {go.name}{nameEx}");
                sw.WriteLine("\t{");
                sw.WriteLine("");


                foreach (ComponentAutoBindTool.BindData data in bindDatas)
                {
                    sw.WriteLine($"\t\tprivate {data.BindCom.GetType().Name} {data.Name};");
                }

                sw.WriteLine("");

                sw.WriteLine("\t\tprivate void GetBindComponents(GameObject go)");
                sw.WriteLine("\t\t{");

                //获取绑定的组件
                sw.WriteLine($"\t\t\tComponentAutoBindTool autoBindTool = go.GetComponent<ComponentAutoBindTool>();");
                sw.WriteLine("");

                //根据索引获取

                for (int i = 0; i < bindDatas.Count; i++)
                {
                    ComponentAutoBindTool.BindData data = bindDatas[i];
                    string filedName = data.Name;
                    sw.WriteLine(
                        $"\t\t\t{filedName} = autoBindTool.GetBindComponent<{data.BindCom.GetType().Name}>({i});");
                }


                sw.WriteLine("\t\t}");

                sw.WriteLine("");

                //根据类型释放
                sw.WriteLine("\t\tprivate void ReleaseBindComponents()");
                sw.WriteLine("\t\t{");

                sw.WriteLine($"\t\t\t//可以根据需要在这里添加代码，位置UIFormCodeGenerator.cs GenAutoBindCode()函数");

                sw.WriteLine("\t\t}");

                sw.WriteLine("");

                sw.WriteLine("\t}");

                sw.WriteLine("}");
            }
        }

        private static void GenAutoItemBindCode(GameObject go, List<ComponentAutoBindTool.BindData> bindDatas,
            AutobindToolConfigWindow.GenCodeType type,
            string codePath, string clsName, string nameSpace, string nameEx = "")
        {
            ComponentAutoBindTool bindTool = go.GetComponent<ComponentAutoBindTool>();
            if (bindTool == null)
            {
                return;
            }

            if (!Directory.Exists($"{codePath}/BindComponents/"))
            {
                Directory.CreateDirectory($"{codePath}/BindComponents/");
            }

            using (StreamWriter sw = new StreamWriter($"{codePath}/BindComponents/{clsName}{nameEx}.BindComponents.cs"))
            {
                sw.WriteLine("using UnityEngine;");
                sw.WriteLine("using Autobind;");
                sw.WriteLine("using UnityEngine.UI;");
                sw.WriteLine("");

                sw.WriteLine("//自动生成于：" + DateTime.Now);

                //命名空间
                sw.WriteLine("namespace " + nameSpace);
                sw.WriteLine("{");
                sw.WriteLine("");

                //类名
                sw.WriteLine($"\tpublic partial class {go.name}{nameEx}");
                sw.WriteLine("\t{");
                sw.WriteLine("");


                foreach (ComponentAutoBindTool.BindData data in bindDatas)
                {
                    sw.WriteLine($"\t\tprivate {data.BindCom.GetType().Name} {data.Name};");
                }

                sw.WriteLine("");

                sw.WriteLine("\t\tprivate void GetBindComponents(GameObject go)");
                sw.WriteLine("\t\t{");

                //获取绑定的组件
                sw.WriteLine($"\t\t\tComponentAutoBindTool autoBindTool = go.GetComponent<ComponentAutoBindTool>();");
                sw.WriteLine("");

                //根据索引获取

                for (int i = 0; i < bindDatas.Count; i++)
                {
                    ComponentAutoBindTool.BindData data = bindDatas[i];
                    string filedName = data.Name;
                    sw.WriteLine(
                        $"\t\t\t{filedName} = autoBindTool.GetBindComponent<{data.BindCom.GetType().Name}>({i});");
                }


                sw.WriteLine("\t\t}");

                sw.WriteLine("");

                // 根据类型释放，这里好像暂时不需要释放
                sw.WriteLine("\t\tprivate void ReleaseBindComponents()");
                sw.WriteLine("\t\t{");
                sw.WriteLine($"\t\t\t//可以根据需要在这里添加代码，位置UIFormCodeGenerator.cs 373行");
                sw.WriteLine("\t\t}");
                //
                sw.WriteLine("");
                //
                sw.WriteLine("\t}");

                sw.WriteLine("}");
            }
        }

        #endregion
    }
}