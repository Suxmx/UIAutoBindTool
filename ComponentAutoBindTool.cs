using System;
using System.Collections.Generic;
using UnityEngine;

namespace Autobind
{
    /**
     * @api {class} werewolf/Assets/Scripts/Main/UnityNative/Utility/ComponentAutoBindTool ComponentAutoBindTool
     * @apiGroup Utility/ComponentAutoBindTool
     * @apiDescription 可挂载，用于组件的自动绑定，核心的代码在ComponentAutoBindToolInspector.cs里面
     */
    public class ComponentAutoBindTool : MonoBehaviour
    {


#if UNITY_EDITOR
        [Serializable]
        public class BindData
        {
            public BindData()
            {
            }

            public BindData(string name, Component bindCom)
            {
                Name = name;
                BindCom = bindCom;
            }

            public string Name;
            public Component BindCom;
        }

        public List<BindData> BindDatas = new List<BindData>();
#endif

        [SerializeField]
        private List<Component> m_BindComs = new List<Component>();


        public T GetBindComponent<T>(int index) where T : Component
        {
            if (index >= m_BindComs.Count)
            {
                throw new Exception("索引无效");
                //Debug.LogError("索引无效");
            }

            T bindCom = m_BindComs[index] as T;

            if (bindCom == null)
            {
                throw new Exception("类型无效");
                //Debug.LogError("类型无效");
            }

            return bindCom;
        }

        public void Clear()
        {
            m_BindComs.Clear();
            //BindDatas.Clear();
        }
    }
}