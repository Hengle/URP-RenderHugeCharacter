﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace GameWish.Game
{
    public class AnimMapBakerWindow : EditorWindow
    {

        private enum SaveStrategy
        {
            AnimMap,//only anim map
            Mat,//with shader
        }

        #region FIELDS

        private static GameObject _targetGo;
        private static AnimMapBaker _baker;
        private static string _path = "Art/Bake";
        private static string _subPath = "SubPath";
        private static SaveStrategy _stratege = SaveStrategy.AnimMap;
        private static Shader _animMapShader;

        #endregion


        #region  METHODS

        [MenuItem("Window/AnimMapBaker")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(AnimMapBakerWindow));
            // _baker = null;//new AnimMapBaker();
            // _animMapShader = Shader.Find("XHH/AnimMapShader");
            _targetGo = null;
        }

        private void OnGUI()
        {
            _targetGo = (GameObject)EditorGUILayout.ObjectField(_targetGo, typeof(GameObject), true);
            _subPath = _targetGo == null ? _subPath : _targetGo.name;
            EditorGUILayout.LabelField(string.Format($"output path:{Path.Combine(_path, _subPath)}"));
            _path = EditorGUILayout.TextField(_path);
            _subPath = EditorGUILayout.TextField(_subPath);

            _stratege = (SaveStrategy)EditorGUILayout.EnumPopup("output type:", _stratege);


            if (GUILayout.Button("Bake"))
            {
                if (_targetGo == null)
                {
                    EditorUtility.DisplayDialog("err", "targetGo is null！", "OK");
                    return;
                }

                if (_baker == null)
                {
                    _baker = new AnimMapBaker();
                }

                if (_animMapShader == null)
                {
                    _animMapShader = Shader.Find("XHH/AnimMapShader");
                }

                _baker.SetAnimData(_targetGo);

                var list = _baker.Bake();

                if (list == null) return;
                foreach (var t in list)
                {
                    var data = t;
                    Save(ref data);
                }

                SaveMesh(_targetGo);
            }
        }

        private void SaveMesh(GameObject obj)
        {

            var renderer = obj.GetComponentInChildren<SkinnedMeshRenderer>();
            if (renderer == null)
            {
                return;
            }

            var sharedMesh = renderer.sharedMesh;
            // Vector3[] vertices = new Vector3[sharedMesh.vertexCount];
            // int[] triangles = new int[sharedMesh.triangles.Length];
            // for (int i = 0; i < sharedMesh.vertexCount; i++)
            // {
            //     vertices[i] = sharedMesh.vertices[i];
            // }

            // for (int i = 0; i < triangles.Length; i++)
            // {
            //     triangles[i] = sharedMesh.triangles[i];
            // }
            var mesh = new Mesh();
            mesh.vertices = sharedMesh.vertices;
            mesh.uv = sharedMesh.uv;
            mesh.triangles = sharedMesh.triangles;


            var folderPath = CreateFolder();
            AssetDatabase.CreateAsset(mesh, Path.Combine(folderPath, obj.name + "Mesh.asset"));
        }

        private void Save(ref BakedData data)
        {
            switch (_stratege)
            {
                case SaveStrategy.AnimMap:
                    SaveAsAsset(ref data);
                    break;
                case SaveStrategy.Mat:
                    SaveAsMat(ref data);
                    break;


            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private Texture2D SaveAsAsset(ref BakedData data)
        {
            var folderPath = CreateFolder();
            var animMap = new Texture2D(data.AnimMapWidth, data.AnimMapHeight, TextureFormat.RGBAHalf, false);
            animMap.wrapMode = TextureWrapMode.Clamp;
            animMap.filterMode = FilterMode.Point;
            animMap.LoadRawTextureData(data.RawAnimMap);
            AssetDatabase.CreateAsset(animMap, Path.Combine(folderPath, data.Name + ".asset"));
            SaveAsTextAsset(ref data);
            return animMap;
        }

        private TextAsset SaveAsTextAsset(ref BakedData data)
        {
            var folderPath = CreateFolder();
            Debug.LogError(data.JsonInfo);
            var text = new TextAsset(data.JsonInfo);
            AssetDatabase.CreateAsset(text, Path.Combine(folderPath, data.Name + "Info.asset"));
            return text;
        }

        private Material SaveAsMat(ref BakedData data)
        {
            if (_animMapShader == null)
            {
                EditorUtility.DisplayDialog("err", "shader is null!!", "OK");
                return null;
            }

            if (_targetGo == null || !_targetGo.GetComponentInChildren<SkinnedMeshRenderer>())
            {
                EditorUtility.DisplayDialog("err", "SkinnedMeshRender is null!!", "OK");
                return null;
            }

            var smr = _targetGo.GetComponentInChildren<SkinnedMeshRenderer>();
            var mat = new Material(_animMapShader);
            var animMap = SaveAsAsset(ref data);
            mat.SetTexture("_MainTex", smr.sharedMaterial.mainTexture);
            mat.SetTexture("_AnimMap", animMap);
            mat.enableInstancing = true;

            var folderPath = CreateFolder();
            AssetDatabase.CreateAsset(mat, Path.Combine(folderPath, data.Name + ".mat"));

            return mat;
        }

        private static string CreateFolder()
        {
            var folderPath = Path.Combine("Assets/" + _path, _subPath);
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/" + _path, _subPath);
            }
            return folderPath;
        }

        #endregion


    }
}