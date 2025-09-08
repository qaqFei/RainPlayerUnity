using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Pool;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.IO;
using System;

namespace Utils {
    public class ZipUtils {
        public static ZipArchiveEntry GetEntry(ZipArchive archive, string entryName) {
            return archive.Entries.FirstOrDefault(entry => entry.FullName == entryName);
        }
    }

    public class MilChartUtils {
        public static double BeatToNumber(double[] beat) {
            return beat != null && beat.Length == 3 ? beat[0] + beat[1] / beat[2] : 0.0;
        }
    }

    public class ResUtils {
        private static readonly HashSet<string> tempFiles = new HashSet<string>();
        private static readonly Dictionary<ObjectPool<GameObject>, HashSet<GameObject>> poolObjects = new();
        
        static ResUtils() {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.playModeStateChanged += (state) => {
                    if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode) {
                        CleanupTempFiles();
                    }
                };
            #else
                AppDomain.CurrentDomain.ProcessExit += (sender, e) => CleanupTempFiles();
            #endif
        }

        private static void CleanupTempFiles() {
            foreach (var file in tempFiles) {
                try {
                    if (File.Exists(file)) {
                        File.Delete(file);
                        Debug.Log($"Deleted temp file: {file}");
                    }
                } catch (Exception e) {
                    Debug.LogWarning($"Failed to delete temp file {file}: {e.Message}");
                }
            }
            tempFiles.Clear();
        }

        public static byte[] ReadAllBytes(Stream stream) {
            using (var memoryStream = new MemoryStream()) {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        private static string GetTempFileName() {
            #if UNITY_EDITOR || UNITY_STANDALONE_WIN
                return Path.GetTempFileName();
            #else
                return Path.Combine(Application.temporaryCachePath, Path.GetRandomFileName());
            #endif
        }

        public static string CreateTempFile(byte[] data) {
            if (data == null) data = new byte[0];
            string path = GetTempFileName();
            lock (tempFiles) tempFiles.Add(path);
            File.WriteAllBytes(path, data);
            return path;
        }

        public static byte[] ReadStreamingAsset(string name) {
            string path = Path.Combine(Application.streamingAssetsPath, name);
            #if UNITY_ANDROID && !UNITY_EDITOR
                return ReadAndroidStreamingAsset(path);
            #else
                return File.ReadAllBytes(path);
            #endif
        }

        private static byte[] ReadAndroidStreamingAsset(string path) {
            using (UnityWebRequest www = UnityWebRequest.Get(path)) {
                www.SendWebRequest();
                while (!www.isDone) { }
                if (www.result != UnityWebRequest.Result.Success) {
                    Debug.LogError($"Failed to read streaming asset {path}: {www.error}");
                    return null;
                }
                return www.downloadHandler.data;
            }
        }

        public static ObjectPool<GameObject> CreateGameObjectPool(GameObject prefab, int defaultCapacity, GameObject parent, int max = 2147483647) {
            var objs = new HashSet<GameObject>();

            Func<GameObject> createFunc = () => {
                GameObject obj = UnityEngine.Object.Instantiate(prefab, parent.transform);
                objs.Add(obj);
                obj.SetActive(false);
                return obj;
            };

            Action<GameObject> actionOnGet = (obj) => {
                obj.SetActive(true);
            };

            Action<GameObject> actionOnRelease = (obj) => {
                obj.SetActive(false);
                obj.transform.SetParent(parent.transform);
            };

            Action<GameObject> actionOnDestroy = (obj) => {
                UnityEngine.Object.Destroy(obj);
                objs.Remove(obj);
            };

            var pool = new ObjectPool<GameObject>(
                createFunc,
                actionOnGet,
                actionOnRelease,
                actionOnDestroy,
                true,
                defaultCapacity,
                max
            );

            poolObjects.Add(pool, objs);
            return pool;
        }

        public static HashSet<GameObject> GetGameObjectPoolObjects(ObjectPool<GameObject> pool) {
            return poolObjects[pool];
        }

        public static void DestroyGameObjectPool(ObjectPool<GameObject> pool) {
            foreach (var obj in GetGameObjectPoolObjects(pool)) {
                UnityEngine.Object.Destroy(obj);
            }

            poolObjects.Remove(pool);
            pool.Clear();
            pool.Dispose();
        }
    }

    public class ColorUtils {
        public static double[] ToRGBA(uint color) {
            return new double[] { (double)((color >> 24) & 0xFF) / 255f, (double)((color >> 16) & 0xFF) / 255f, (double)((color >> 8) & 0xFF) / 255f, (double)(color & 0xFF) / 255f };
        }

        public static uint ToUint(double[] rgba) {
            return (uint)(rgba[0] * 255f) << 24 | (uint)(rgba[1] * 255f) << 16 | (uint)(rgba[2] * 255f) << 8 | (uint)(rgba[3] * 255f);
        }

        // fuck in other places
        // public static double FuckUnityAlphaGammaFix(double value) {
            // return 1.0 - Math.Pow(1.0 - value, 2.2);
        // }
    }

    public class MathUtils {
        public static Vector2 RotatePoint(Vector2 point, double degrees, double len) {
            return new Vector2(
                (float)(len * Math.Cos(degrees * Math.PI / 180f)),
                (float)(len * Math.Sin(degrees * Math.PI / 180f))
            );
        }

        public class WebCanvas2DTransform {
            public double[] matrix;

            public WebCanvas2DTransform(double[] matrix = null) {
                if (matrix == null) {
                    this.matrix = new double[] { 1, 0, 0, 1, 0, 0 };
                } else {
                    this.matrix = matrix;
                }
            }

            public WebCanvas2DTransform resetTransform() {
                matrix = new double[] { 1, 0, 0, 1, 0, 0 };
                return this;
            }

            public WebCanvas2DTransform setTransform(double a, double b, double c, double d, double e, double f) {
                matrix = new double[] { a, b, c, d, e, f };
                return this;
            }

            public WebCanvas2DTransform transform(double a, double b, double c, double d, double e, double f) {
                matrix = new double[] {
                    matrix[0] * a + matrix[2] * b,
                    matrix[1] * a + matrix[3] * b,
                    matrix[0] * c + matrix[2] * d,
                    matrix[1] * c + matrix[3] * d,
                    matrix[0] * e + matrix[2] * f + matrix[4],
                    matrix[1] * e + matrix[3] * f + matrix[5]
                };
                return this;
            }

            public WebCanvas2DTransform scale(double x, double y) {
                transform(x, 0, 0, y, 0, 0);
                return this;
            }

            public WebCanvas2DTransform translate(double x, double y) {
                transform(1, 0, 0, 1, x, y);
                return this;
            }

            public WebCanvas2DTransform rotate(double angle) {
                transform(Math.Cos(angle), Math.Sin(angle), -Math.Sin(angle), Math.Cos(angle), 0, 0);
                return this;
            }

            public WebCanvas2DTransform rotateDegree(double degrees) {
                rotate(Math.PI * degrees / 180);
                return this;
            }

            public Vector2 getPoint(double x, double y) {
                return new Vector2(
                    (float)(matrix[0] * x + matrix[2] * y + matrix[4]),
                    (float)(matrix[1] * x + matrix[3] * y + matrix[5])
                );
            }

            public Vector2[] getRectPoints(double x, double y, double width, double height) {
                return new Vector2[] {
                    getPoint(x, y),
                    getPoint(x + width, y),
                    getPoint(x + width, y + height),
                    getPoint(x, y + height)
                };
            }

            public Vector2[] getCRectPoints(double x, double y, double width, double height) {
                x -= width / 2;
                y -= height / 2;
                return getRectPoints(x, y, width, height);
            }

            public WebCanvas2DTransform getInverse() {
                var det = matrix[0] * matrix[3] - matrix[1] * matrix[2];
                var inv_det = det == 0 ? 1e9 : 1 / det;
                var inv = new double[] {
                    matrix[3] * inv_det,
                    -matrix[1] * inv_det,
                    -matrix[2] * inv_det,
                    matrix[0] * inv_det,
                    (matrix[2] * matrix[5] - matrix[3] * matrix[4]) * inv_det,
                    (matrix[1] * matrix[4] - matrix[0] * matrix[5]) * inv_det
                };
                return new WebCanvas2DTransform(inv);
            }
        }

        public static double Fixorp(double value) {
            return value < 0.0 ? 0.0 : (value > 1.0 ? 1.0 : value);
        }

        public static bool polygonInScreen(double w, double h, Vector2[] polygon) {
            return polygonIntersect(getScreenPoints(w, h), polygon);
        }

        public static Vector2[] getScreenPoints(double w, double h) {
            return new Vector2[] {
                new Vector2(0, 0),
                new Vector2((float)w, 0),
                new Vector2((float)w, (float)h),
                new Vector2(0, (float)h)
            };
        }

        private static bool boolLstAny(bool[] lst) {
            foreach (var b in lst) {
                if (b) return true;
            }
            return false;
        }

        public static bool polygonIntersect(Vector2[] p1, Vector2[] p2) {
            return (
                boolLstAny(batch_is_intersect(polygon2lines(p1), polygon2lines(p2)))
                || pointInOtherPolygon(p1, p2)
                || pointInOtherPolygon(p2, p1)
            );
        }

        public static bool pointInOtherPolygon(Vector2[] p1, Vector2[] p2) {
            var res = new bool[p2.Length];
            for (int i = 0; i < p2.Length; i++) {
                res[i] = pointInPolygon(p1, p2[i]);
            }
            return boolLstAny(res);
        }

        public static Vector2[][] polygon2lines(Vector2[] polygon) {
            var res = new Vector2[polygon.Length][];
            res[0] = new Vector2[] { polygon[polygon.Length - 1], polygon[0] };
            for (int i = 0; i < polygon.Length - 1; i++) {
                res[i + 1] = new Vector2[] { polygon[i], polygon[i + 1] };
            }
            return res;
        }

        public static bool[] batch_is_intersect(Vector2[][] lines1, Vector2[][] lines2) {
            var res = new bool[lines1.Length * lines2.Length];
            for (int i = 0; i < lines1.Length; i++) {
                for (int j = 0; j < lines2.Length; j++) {
                    res[i * lines2.Length + j] = is_intersect(lines1[i], lines2[j]);
                }
            }
            return res;
        }

        public static bool is_intersect(Vector2[] line_1, Vector2[] line_2) {
            return !(
                Math.Max(line_1[0].x, line_1[1].x) < Math.Max(line_2[0].x, line_2[1].x) ||
                Math.Max(line_2[0].x, line_2[1].x) < Math.Max(line_1[0].x, line_1[1].x) ||
                Math.Max(line_1[0].y, line_1[1].y) < Math.Max(line_2[0].y, line_2[1].y) ||
                Math.Max(line_2[0].y, line_2[1].y) < Math.Max(line_1[0].y, line_1[1].y)
            );
        }

        public static bool pointInPolygon(Vector2[] polygon, Vector2 point) {
            var n = polygon.Length;
            var j = n - 1;
            var res = false;
            for (int i = 0; i < n; i++) {
                if (
                    polygon[i].y > point.y != polygon[j].y > point.y &&
                    (
                        point.x < (
                            (polygon[j].x - polygon[i].x)
                            * (point.y - polygon[i].y)
                            / (polygon[j].y - polygon[i].y)
                            + polygon[i].x
                        )
                    )
                ) res = !res;
                j = i;
            }
            return res;
        }

        public static double getLineLength(double x1, double y1, double x2, double y2) {
            return Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
        }
    }
}
