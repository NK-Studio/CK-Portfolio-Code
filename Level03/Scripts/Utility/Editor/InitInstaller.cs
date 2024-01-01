#if UNITY_EDITOR

#if UNITY_EDITOR_WIN
using System;
#elif UNITY_EDITOR_OSX
using UnityEngine;
#endif
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEditor;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;


namespace NKStudio
{
    public class InitInstaller : Editor
    {
#if UNITY_EDITOR_WIN
        private const string InstallPath = "Assets/StreamingAssets/";
        private const string ZipFilePath = "Assets/HideZip~/OpeningMovie.zip";
#elif UNITY_EDITOR_OSX
        private const string InstallPath = "StreamingAssets/";
        private const string ZipFilePath = "HideZip~/OpeningMovie.zip";
#endif
        private const string TargetFilePath = "Assets/StreamingAssets/OpeningMovie.mp4";

        private static MethodInfo _clearMethodInfo;

        [MenuItem("Tools/Movie Download/Start Download")]
        private static void CheckHasBandizip()
        {
#if UNITY_EDITOR_WIN
            var hasBandizip = IsProgramInstalled("Bandizip.exe");
            
            if (!hasBandizip)
            {
                Debug.LogWarning("Bandizip이 설치되어 있지 않습니다.");
                return;
            }
#elif UNITY_EDITOR_OSX
            var hasBandizip = CheckFileExists("/Applications/Bandizip.app/Contents/MacOS/Bandizip");

            if (!hasBandizip)
            {
                Debug.LogWarning(
                    "Bandizip이 설치되어 있지 않습니다.\n<color=#ffff00ff>애플 스토어에서 설치해주세요.</color>로 이동하여 설치해주세요.");
                return;
            }
#endif

            // Zip을 이미 해제 완료했는지 체크합니다.
            bool findMovie = CheckFileExists(TargetFilePath);

            if (findMovie)
            {
                TargetingVideoClip();
                Debug.LogWarning("이미 영상이 다운로드 되어 있습니다.");
                return;
            }

            StartInstall();
        }

        private static void StartInstall()
        {
            // Zip해제 명령어를 실행합니다.
            string destPath = InstallPath;
            string targetPath = ZipFilePath;

#if UNITY_EDITOR_WIN
            string command = @$"Bandizip.exe x -o:{destPath} {targetPath}";
            ExecuteCMD(command);
#elif UNITY_EDITOR_OSX
            string command =
                $"open /Applications/Bandizip.app --args x -o:{Application.dataPath}/{destPath} {Application.dataPath}/{targetPath}";
            string appleScript = $"{command}";
            ExecuteTerminal(appleScript);
#endif
            ResultShow().Forget();
        }

        /// <summary>
        /// 100프레임이 지났을 때 반디집이 켜져있는지에 따라 Zip해제 가능 여부를 알려주는 함수
        /// </summary>
        private static async UniTaskVoid ResultShow()
        {
            await UniTask.Delay(300);

            AssetDatabase.ImportAsset(TargetFilePath);
            bool isInstalled = IsProgramInstalled("Bandizip");

            if (isInstalled)
                Debug.Log("<color=green>다운로드를 완료되었습니다.</color>");
            else
            {
#if UNITY_EDITOR_WIN
                Debug.LogWarning("반디집이 설치되어 있지 않습니다.");
#elif UNITY_EDITOR_OSX
                Debug.LogWarning("터미널이 켜지지 않았습니다.");
#endif
                // 반디집이 설치되어 있지 않다고 말을하지만, 프로그램 포커스 이슈때문에 한번 더 체크해봅니다.
                bool findMovie = CheckFileExists(TargetFilePath);

                if (findMovie)
                {
                    ClearLog();
                    Debug.Log("<color=#11f911>다운로드를 완료되었습니다.</color>");
                    AssetDatabase.ImportAsset(TargetFilePath);
                    TargetingVideoClip();
                }
            }
        }

        private static void TargetingVideoClip()
        {
            var clip = AssetDatabase.LoadAssetAtPath<Object>(TargetFilePath);

            if (clip != null) 
                Selection.activeObject = clip;
        }

        /// <summary>
        /// 유니티 콘솔을 깨끗하게 비웁니다.
        /// </summary>
        private static void ClearLog()
        {
            if (_clearMethodInfo == null)
            {
                var assembly = Assembly.GetAssembly(typeof(Editor));
                var type = assembly.GetType("UnityEditor.LogEntries");
                _clearMethodInfo = type.GetMethod("Clear");
            }

            _clearMethodInfo?.Invoke(new object(), null);
        }

        /// <summary>
        /// 현재 해당 프로그램이 실행되고 있는지 체크하는 함수
        /// </summary>
        /// <param name="programName"></param>
        /// <returns></returns>
        private static bool IsProgramInstalled(string programName)
        {
            Process[] processes = Process.GetProcessesByName(programName);
            return processes.Length > 0;
        }

        /// <summary>
        /// 경로에 파일이 존재하는지 체크하는 함수
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private static bool CheckFileExists(string filePath)
        {
            return File.Exists(filePath);
        }

#if UNITY_EDITOR_WIN
        /// <summary>
        /// textKey : 실행할 명령어 
        /// </summary>
        private static void ExecuteCMD(string textKey)
        {
            ProcessStartInfo pri = new();
            Process pro = new();

            pri.FileName = @"cmd.exe";
            pri.CreateNoWindow = true;
            pri.UseShellExecute = false;

            pri.RedirectStandardInput = true; //표준 출력을 리다이렉트
            pri.RedirectStandardOutput = true;
            pri.RedirectStandardError = true;

            pro.StartInfo = pri;
            pro.Start(); //어플리케이션 실행

            pro.StandardInput.Write(textKey + Environment.NewLine);
            pro.StandardInput.Close();

            StreamReader sr = pro.StandardOutput;

            sr.ReadToEnd();
            pro.WaitForExit();
            pro.Close();
        }
#elif UNITY_EDITOR_OSX
        /// <summary>
        /// textKey : 실행할 명령어 
        /// </summary>
        private static void ExecuteTerminal(string command)
        {
            // AppleScript 명령어 생성
            string appleScript =
                $"tell application \"Terminal\" to do script \"cd {Application.dataPath} && {command}\"";

            // 셸 명령어 실행을 위해 Process 객체 생성
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/usr/bin/osascript",
                    Arguments = $"-e '{appleScript}'",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                }
            };

            // 외부 프로세스 시작
            process.Start();
            process.WaitForExit();
            process.Close();
        }
#endif
    }
}
#endif