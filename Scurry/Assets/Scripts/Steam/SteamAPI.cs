using System;
using System.Runtime.InteropServices;

namespace Scurry.Steam
{
    public static class SteamAPI
    {
#if UNITY_STANDALONE_WIN
        private const string NATIVE_LIB = "steam_api64";
#elif UNITY_STANDALONE_LINUX
        private const string NATIVE_LIB = "libsteam_api";
#elif UNITY_STANDALONE_OSX
        private const string NATIVE_LIB = "libsteam_api";
#else
        private const string NATIVE_LIB = "steam_api64";
#endif

        [DllImport(NATIVE_LIB, EntryPoint = "SteamAPI_Init", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool Init();

        [DllImport(NATIVE_LIB, EntryPoint = "SteamAPI_Shutdown", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Shutdown();

        [DllImport(NATIVE_LIB, EntryPoint = "SteamAPI_RunCallbacks", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RunCallbacks();

        [DllImport(NATIVE_LIB, EntryPoint = "SteamAPI_RestartAppIfNecessary", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool RestartAppIfNecessary(uint appId);

        [DllImport(NATIVE_LIB, EntryPoint = "SteamAPI_IsSteamRunning", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool IsSteamRunning();
    }

    public static class SteamUserStats
    {
#if UNITY_STANDALONE_WIN
        private const string NATIVE_LIB = "steam_api64";
#elif UNITY_STANDALONE_LINUX
        private const string NATIVE_LIB = "libsteam_api";
#elif UNITY_STANDALONE_OSX
        private const string NATIVE_LIB = "libsteam_api";
#else
        private const string NATIVE_LIB = "steam_api64";
#endif

        [DllImport(NATIVE_LIB, EntryPoint = "SteamAPI_ISteamUserStats_RequestCurrentStats", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool Internal_RequestCurrentStats(IntPtr steamUserStats);

        [DllImport(NATIVE_LIB, EntryPoint = "SteamAPI_ISteamUserStats_SetAchievement", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool Internal_SetAchievement(IntPtr steamUserStats, [MarshalAs(UnmanagedType.LPStr)] string achievementName);

        [DllImport(NATIVE_LIB, EntryPoint = "SteamAPI_ISteamUserStats_GetAchievement", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool Internal_GetAchievement(IntPtr steamUserStats, [MarshalAs(UnmanagedType.LPStr)] string achievementName, [MarshalAs(UnmanagedType.I1)] out bool achieved);

        [DllImport(NATIVE_LIB, EntryPoint = "SteamAPI_ISteamUserStats_StoreStats", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool Internal_StoreStats(IntPtr steamUserStats);

        [DllImport(NATIVE_LIB, EntryPoint = "SteamAPI_ISteamUserStats_ClearAchievement", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool Internal_ClearAchievement(IntPtr steamUserStats, [MarshalAs(UnmanagedType.LPStr)] string achievementName);

        [DllImport(NATIVE_LIB, EntryPoint = "SteamAPI_SteamUserStats_v012", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetSteamUserStats();

        public static bool RequestCurrentStats()
        {
            var ptr = GetSteamUserStats();
            if (ptr == IntPtr.Zero) return false;
            return Internal_RequestCurrentStats(ptr);
        }

        public static bool SetAchievement(string name)
        {
            var ptr = GetSteamUserStats();
            if (ptr == IntPtr.Zero) return false;
            return Internal_SetAchievement(ptr, name);
        }

        public static bool GetAchievement(string name, out bool achieved)
        {
            achieved = false;
            var ptr = GetSteamUserStats();
            if (ptr == IntPtr.Zero) return false;
            return Internal_GetAchievement(ptr, name, out achieved);
        }

        public static bool StoreStats()
        {
            var ptr = GetSteamUserStats();
            if (ptr == IntPtr.Zero) return false;
            return Internal_StoreStats(ptr);
        }

        public static bool ClearAchievement(string name)
        {
            var ptr = GetSteamUserStats();
            if (ptr == IntPtr.Zero) return false;
            return Internal_ClearAchievement(ptr, name);
        }
    }

    public static class SteamFriends
    {
#if UNITY_STANDALONE_WIN
        private const string NATIVE_LIB = "steam_api64";
#elif UNITY_STANDALONE_LINUX
        private const string NATIVE_LIB = "libsteam_api";
#elif UNITY_STANDALONE_OSX
        private const string NATIVE_LIB = "libsteam_api";
#else
        private const string NATIVE_LIB = "steam_api64";
#endif

        [DllImport(NATIVE_LIB, EntryPoint = "SteamAPI_SteamFriends_v017", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetSteamFriends();

        [DllImport(NATIVE_LIB, EntryPoint = "SteamAPI_ISteamFriends_SetRichPresence", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool Internal_SetRichPresence(IntPtr steamFriends, [MarshalAs(UnmanagedType.LPStr)] string key, [MarshalAs(UnmanagedType.LPStr)] string value);

        [DllImport(NATIVE_LIB, EntryPoint = "SteamAPI_ISteamFriends_ClearRichPresence", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Internal_ClearRichPresence(IntPtr steamFriends);

        public static bool SetRichPresence(string key, string value)
        {
            var ptr = GetSteamFriends();
            if (ptr == IntPtr.Zero) return false;
            return Internal_SetRichPresence(ptr, key, value);
        }

        public static void ClearRichPresence()
        {
            var ptr = GetSteamFriends();
            if (ptr == IntPtr.Zero) return;
            Internal_ClearRichPresence(ptr);
        }
    }

    public static class SteamRemoteStorage
    {
#if UNITY_STANDALONE_WIN
        private const string NATIVE_LIB = "steam_api64";
#elif UNITY_STANDALONE_LINUX
        private const string NATIVE_LIB = "libsteam_api";
#elif UNITY_STANDALONE_OSX
        private const string NATIVE_LIB = "libsteam_api";
#else
        private const string NATIVE_LIB = "steam_api64";
#endif

        [DllImport(NATIVE_LIB, EntryPoint = "SteamAPI_SteamRemoteStorage_v016", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetSteamRemoteStorage();

        [DllImport(NATIVE_LIB, EntryPoint = "SteamAPI_ISteamRemoteStorage_FileWrite", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool Internal_FileWrite(IntPtr storage, [MarshalAs(UnmanagedType.LPStr)] string fileName, byte[] data, int dataLength);

        [DllImport(NATIVE_LIB, EntryPoint = "SteamAPI_ISteamRemoteStorage_FileRead", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Internal_FileRead(IntPtr storage, [MarshalAs(UnmanagedType.LPStr)] string fileName, byte[] data, int dataToRead);

        [DllImport(NATIVE_LIB, EntryPoint = "SteamAPI_ISteamRemoteStorage_FileExists", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool Internal_FileExists(IntPtr storage, [MarshalAs(UnmanagedType.LPStr)] string fileName);

        [DllImport(NATIVE_LIB, EntryPoint = "SteamAPI_ISteamRemoteStorage_GetFileSize", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Internal_GetFileSize(IntPtr storage, [MarshalAs(UnmanagedType.LPStr)] string fileName);

        public static bool FileWrite(string fileName, byte[] data)
        {
            var ptr = GetSteamRemoteStorage();
            if (ptr == IntPtr.Zero) return false;
            return Internal_FileWrite(ptr, fileName, data, data.Length);
        }

        public static byte[] FileRead(string fileName)
        {
            var ptr = GetSteamRemoteStorage();
            if (ptr == IntPtr.Zero) return null;
            if (!Internal_FileExists(ptr, fileName)) return null;
            int size = Internal_GetFileSize(ptr, fileName);
            if (size <= 0) return null;
            byte[] buffer = new byte[size];
            int bytesRead = Internal_FileRead(ptr, fileName, buffer, size);
            if (bytesRead != size) return null;
            return buffer;
        }

        public static bool FileExists(string fileName)
        {
            var ptr = GetSteamRemoteStorage();
            if (ptr == IntPtr.Zero) return false;
            return Internal_FileExists(ptr, fileName);
        }
    }
}
