/*
* ┌──────────────────────────────────┐
* │  描    述: 消息处理中心脚本                      
* │  类    名: MessageCenter.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;

namespace Common
{
    public class MessageCenter
    {
        private Dictionary<string, System.Action<object>> msgDic;//存储普通消息的字典
        private Dictionary<string, System.Action<object>> tempMsgDic;//存储临时消息的字典
        private Dictionary<System.Object, Dictionary<string, System.Action<object>>> objMsgDic;//存储对象消息的字典

        public MessageCenter()
        {
            msgDic = new Dictionary<string, System.Action<object>>();
            tempMsgDic = new Dictionary<string, System.Action<object>>();
            objMsgDic = new Dictionary<System.Object, Dictionary<string, System.Action<object>>>();
        }
        
        //添加事件
        public void AddEvent(string eventName, System.Action<object> callback)
        {
            if (msgDic.ContainsKey(eventName))
            {
                msgDic[eventName] += callback;
            }
            else
            {
                msgDic.Add(eventName, callback);
            }
        }
        
        //移除事件
        public void RemoveEvent(string eventName, System.Action<object> callback)
        {
            if (msgDic.ContainsKey(eventName))
            {
                msgDic[eventName] -= callback;
                if (msgDic[eventName] == null)
                    msgDic.Remove(eventName);
            }
        }
        
        //执行事件
        public void PostEvent(string eventName, object arg = null)
        {
            if (msgDic.ContainsKey(eventName))
            {
                msgDic[eventName]?.Invoke(arg);
            }
        }
        
        //添加对象事件
        public void AddEvent(System.Object listenerObj, string eventName, System.Action<object> callback)
        {
            //已经存在该对象的事件
            if (objMsgDic.ContainsKey(listenerObj))
            {
                if(objMsgDic[listenerObj].ContainsKey(eventName))
                {
                    objMsgDic[listenerObj][eventName] += callback;
                }
                else
                {
                    objMsgDic[listenerObj].Add(eventName, callback);
                }
            }
            else
            {
                Dictionary<string, System.Action<object>> _tempDic = new Dictionary<string, System.Action<object>>();
                _tempDic.Add(eventName, callback);
                objMsgDic.Add(listenerObj, _tempDic);
            }
        }
        
        //移除对象事件
        public void RemoveEvent(System.Object listenerObj, string eventName, System.Action<object> callback)
        {
            if (objMsgDic.ContainsKey(listenerObj) && objMsgDic[listenerObj].ContainsKey(eventName))
            {
                objMsgDic[listenerObj][eventName] -= callback;
                if (objMsgDic[listenerObj][eventName] == null)
                    objMsgDic[listenerObj].Remove(eventName);
                if (objMsgDic[listenerObj].Count == 0)
                    objMsgDic.Remove(listenerObj);
            }
        }
        
        //移除对象的所有事件
        public void RemoveAllEvent(System.Object listenerObj)
        {
            if (objMsgDic.ContainsKey(listenerObj))
            {
                objMsgDic.Remove(listenerObj);
            }
        }
        
        //执行对象事件
        public void PostEvent(System.Object listenerObj, string eventName, object arg = null)
        {
            if (objMsgDic.ContainsKey(listenerObj) && objMsgDic[listenerObj].ContainsKey(eventName))
            {
                objMsgDic[listenerObj][eventName]?.Invoke(arg);
            }
        }

        //添加临时事件
        public void AddTempEvent(string eventName, System.Action<object> callback)
        {
            if (tempMsgDic.ContainsKey(eventName))
            {
                tempMsgDic[eventName] = callback;//覆盖之前的事件
            }
            else
            {
                tempMsgDic.Add(eventName, callback);
            }
        }
        
        //执行临时事件
        public void PostTempEvent(string eventName, object arg = null)
        {
            if (tempMsgDic.ContainsKey(eventName))
            {
                tempMsgDic[eventName]?.Invoke(arg);
                tempMsgDic[eventName] = null;
                tempMsgDic.Remove(eventName);//执行完后移除事件
            }
        }
    }
}