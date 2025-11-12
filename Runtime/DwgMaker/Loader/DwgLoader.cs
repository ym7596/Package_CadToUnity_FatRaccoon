using System;
using System.IO;
using ACadSharp;
using UnityEngine;

namespace CadToUnityPlugin
{
    public class DwgLoader
    { 
        private NetworkManager _networkManager;

        private const string DwgExtension = ".dwg";
        
        /// <summary>
        /// streamingAssets folder 에서 dwg 데이터 읽고 CadDocument 로 반환
        /// </summary>
        public async Awaitable<CadDocument> LoadStreamingAssetsFolderDwgAsync(string fileName)
        {
            Debug.Log("LoadDWG");
            if (!fileName.EndsWith(DwgExtension))
            {
                fileName = $"{fileName}{DwgExtension}";
            }
            var filePath = Path.Combine(Application.streamingAssetsPath, fileName);
#if UNITY_EDITOR
            var dwgData = File.ReadAllBytes(filePath);
            return ReadDwg(dwgData);
#elif UNITY_WEBGL
            return await LoadRemoteDwgAsync(filePath);
#else
            return await LoadRemoteDwgAsync(filePath);
#endif
        }
        
        public async Awaitable<CadDocument> LoadDwgWithFilePickerAsync()
        { 


#if UNITY_EDITOR
            var path = UnityEditor.EditorUtility.OpenFilePanel("Select DWG file", "", "dwg");
            return await LoadLocalDwgAsync(path);
#elif UNITY_STANDALONE_WIN
    #if SIMPLE_FILE_BROWSER
            SimpleFileBrowser.FileBrowser.SetFilters(true, new SimpleFileBrowser.FileBrowser.Filter("DWG", ".dwg"));
            SimpleFileBrowser.FileBrowser.SetDefaultFilter(".dwg");

            await UniTask.SwitchToMainThread();
            await SimpleFileBrowser.FileBrowser.WaitForLoadDialog(
                SimpleFileBrowser.FileBrowser.PickMode.Files,
                false,
                null,
                null,
                "Select DWG file",
                "Load"
            ).ToUniTask(); // ← FromCoroutine 대신 ToUniTask 사용

            if (SimpleFileBrowser.FileBrowser.Success && SimpleFileBrowser.FileBrowser.Result != null && SimpleFileBrowser.FileBrowser.Result.Length > 0)
            {
                var path = SimpleFileBrowser.FileBrowser.Result[0];
                return await LoadLocalDwgAsync(path);
            }
            Debug.LogWarning("File selection canceled.");
            return null;

    #elif SFB_PRESENT
            var paths = SFB.StandaloneFileBrowser.OpenFilePanel("Select DWG file", "", "dwg", false);
            var path = (paths != null && paths.Length > 0) ? paths[0] : null;
            return await LoadLocalDwgAsync(path);
#else
            Debug.LogError("Windows Standalone에서 파일 피커를 사용하려면 SimpleFileBrowser(define SIMPLE_FILE_BROWSER) 또는 StandaloneFileBrowser(define SFB_PRESENT) 패키지를 추가하세요. 현재는 아무 것도 표시되지 않습니다.");
            return null;
#endif
#else
            Debug.LogError("파일 피커는 현재 Windows 또는 Unity Editor에서만 지원됩니다.");
            return null;
#endif

        }

        /// <summary>
        /// 로컬 경로의 dwg 파일을 읽어 CadDocument 반환
        /// </summary>
        public async Awaitable<CadDocument> LoadLocalDwgAsync(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                Debug.LogWarning("DWG path is null or empty.");
                return null;
            }

            if (!fullPath.EndsWith(DwgExtension, StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"Invalid extension: {fullPath}");
                return null;
            }

            var dwgData = File.ReadAllBytes(fullPath);
            return ReadDwg(dwgData);
        }

        /// <summary>
        /// url 로 dwg 데이터를 다운받아 읽고 CadDocument 로 반환
        /// </summary>
        public async Awaitable<CadDocument> LoadRemoteDwgAsync(string url)
        {
            _networkManager ??= new NetworkManager();
            var dwgData = await _networkManager.Download(url);
            return dwgData != null ? ReadDwg(dwgData) : null;
        }

        /// <summary>
        /// CadDocument 데이터를 읽고 dwg byte array 로 반환
        /// </summary>
        private CadDocument ReadDwg(byte[] dwgData)
        {
            Debug.Log("ReadData");
            using ACadSharp.IO.DwgReader reader = new(new MemoryStream(dwgData));
            return reader.Read();
        }
    }
}