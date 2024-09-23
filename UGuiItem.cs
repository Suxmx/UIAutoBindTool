using UnityEngine;

namespace GameMain
{
    public abstract class UGuiItem : MonoBehaviour
    {
        public abstract void OnInit(object userData);
        
        public abstract void OnOpen(object userData);

        public abstract void OnClose(object userData);
    }
}