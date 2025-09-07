using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Sasa {
    internal class _libSasa {
        private const string DLL_NAME = "sasa";

        [DllImport(DLL_NAME)] public static extern bool play_sfx(IntPtr sfx_ptr, float volume);
        [DllImport(DLL_NAME)] public static extern bool play_music(IntPtr music_ptr, float volume);
        [DllImport(DLL_NAME)] public static extern bool pause_music(IntPtr music_ptr);
        [DllImport(DLL_NAME)] public static extern bool is_music_paused(IntPtr music_ptr);
        [DllImport(DLL_NAME)] public static extern bool seek_music(IntPtr music_ptr, double time);
        [DllImport(DLL_NAME)] public static extern bool set_music_volume(IntPtr music_ptr, float volume);
        [DllImport(DLL_NAME)] public static extern double get_music_position(IntPtr music_ptr);
        [DllImport(DLL_NAME)] public static extern double get_audio_clip_duration(IntPtr clip_ptr);
        [DllImport(DLL_NAME)] public static extern void destroy_manager(IntPtr manager_ptr);
        [DllImport(DLL_NAME)] public static extern void destroy_clip(IntPtr clip_ptr);
        [DllImport(DLL_NAME)] public static extern void destroy_sfx(IntPtr sfx_ptr);
        [DllImport(DLL_NAME)] public static extern void destroy_music(IntPtr music_ptr);
        [DllImport(DLL_NAME)] public static extern IntPtr create_audio_manager();
        [DllImport(DLL_NAME)] public static extern bool recover_if_needed(IntPtr manager_ptr);
        [DllImport(DLL_NAME)] public static extern IntPtr load_audio_clip(string path);
        [DllImport(DLL_NAME)] public static extern IntPtr create_sfx(IntPtr manager_ptr, IntPtr clip_ptr);
        [DllImport(DLL_NAME)] public static extern IntPtr create_music(IntPtr manager_ptr, IntPtr clip_ptr);
    }

    public class libSasa {
        private static HashSet<IntPtr> managers = new HashSet<IntPtr>();
        private static HashSet<IntPtr> clips = new HashSet<IntPtr>();
        private static HashSet<IntPtr> sfxs = new HashSet<IntPtr>();
        private static HashSet<IntPtr> musics = new HashSet<IntPtr>();
        private static readonly object setLock = new object();

        public static bool play_sfx(IntPtr sfx_ptr, float volume) {
            if (!sfxs.Contains(sfx_ptr)) return false;
            return _libSasa.play_sfx(sfx_ptr, volume);
        }

        public static bool play_music(IntPtr music_ptr, float volume) {
            if (!musics.Contains(music_ptr)) return false;
            return _libSasa.play_music(music_ptr, volume);
        }

        public static bool pause_music(IntPtr music_ptr) {
            if (!musics.Contains(music_ptr)) return false;
            return _libSasa.pause_music(music_ptr);
        }

        public static bool is_music_paused(IntPtr music_ptr) {
            if (!musics.Contains(music_ptr)) return false;
            return _libSasa.is_music_paused(music_ptr);
        }

        public static bool seek_music(IntPtr music_ptr, double time) {
            if (!musics.Contains(music_ptr)) return false;
            return _libSasa.seek_music(music_ptr, time);
        }

        public static bool set_music_volume(IntPtr music_ptr, float volume) {
            if (!musics.Contains(music_ptr)) return false;
            return _libSasa.set_music_volume(music_ptr, volume);
        }

        public static double get_music_position(IntPtr music_ptr) {
            if (!musics.Contains(music_ptr)) return -1;
            return _libSasa.get_music_position(music_ptr);
        }

        public static double get_audio_clip_duration(IntPtr clip_ptr) {
            if (!clips.Contains(clip_ptr)) return -1;
            return _libSasa.get_audio_clip_duration(clip_ptr);
        }

        public static void destroy_manager(IntPtr manager_ptr) {
            if (!managers.Contains(manager_ptr)) return;
            _libSasa.destroy_manager(manager_ptr);
            lock (setLock) managers.Remove(manager_ptr);
        }

        public static void destroy_clip(IntPtr clip_ptr) {
            if (!clips.Contains(clip_ptr)) return;
            _libSasa.destroy_clip(clip_ptr);
            lock (setLock) clips.Remove(clip_ptr);
        }

        public static void destroy_sfx(IntPtr sfx_ptr) {
            if (!sfxs.Contains(sfx_ptr)) return;
            _libSasa.destroy_sfx(sfx_ptr);
            lock (setLock) sfxs.Remove(sfx_ptr);
        }

        public static void destroy_music(IntPtr music_ptr) {
            if (!musics.Contains(music_ptr)) return;
            _libSasa.destroy_music(music_ptr);
            lock (setLock) musics.Remove(music_ptr);
        }

        public static IntPtr create_audio_manager() {
            IntPtr manager_ptr = _libSasa.create_audio_manager();
            lock (setLock) managers.Add(manager_ptr);
            return manager_ptr;
        }

        public static bool recover_if_needed(IntPtr manager_ptr) {
            if (!managers.Contains(manager_ptr)) return false;
            return _libSasa.recover_if_needed(manager_ptr);
        }

        public static IntPtr load_audio_clip(string path) {
            IntPtr clip_ptr = _libSasa.load_audio_clip(path);
            if (clip_ptr == IntPtr.Zero) return IntPtr.Zero;
            lock (setLock) clips.Add(clip_ptr);
            return clip_ptr;
        }

        public static IntPtr create_sfx(IntPtr manager_ptr, IntPtr clip_ptr) {
            if (!managers.Contains(manager_ptr) || !clips.Contains(clip_ptr)) return IntPtr.Zero;
            IntPtr sfx_ptr = _libSasa.create_sfx(manager_ptr, clip_ptr);
            lock (setLock) sfxs.Add(sfx_ptr);
            return sfx_ptr;
        }

        public static IntPtr create_music(IntPtr manager_ptr, IntPtr clip_ptr) {
            if (!managers.Contains(manager_ptr) || !clips.Contains(clip_ptr)) return IntPtr.Zero;
            IntPtr music_ptr = _libSasa.create_music(manager_ptr, clip_ptr);
            lock (setLock) musics.Add(music_ptr);
            return music_ptr;
        }
    }
}
