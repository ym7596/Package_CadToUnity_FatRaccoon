using UnityEngine;
using UnityEngine.Networking;

namespace CadToUnityPlugin
{
    public class NetworkManager
    {
        /// <summary>
        /// 다운로드 dwg data
        /// </summary>
        public async Awaitable<byte[]> Download(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            Debug.Log("Start download");
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                var operation = www.SendWebRequest();

                while (!operation.isDone)
                    await Awaitable.NextFrameAsync();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Error: " + www.error);
                    return null;
                }
                else
                {
                    Debug.Log("Download success");
                    return www.downloadHandler.data;
                }
            }
        }
    }
}