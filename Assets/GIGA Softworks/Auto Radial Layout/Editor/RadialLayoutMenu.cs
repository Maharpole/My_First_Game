﻿#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using GIGA.AutoRadialLayout.QuerySystem;

namespace GIGA.AutoRadialLayout.Editor
{
	public class RadialLayoutMenu : MonoBehaviour
	{
		private const string layoutPrefabPath = "Assets/GIGA Softworks/Auto Radial Layout/Prefabs/prefab_RadialLayout.prefab";
		private const string radialMenuPrefabPath = "Assets/GIGA Softworks/Auto Radial Layout/Prefabs/RadialMenu/prefab_RadialMenu.prefab";

		[MenuItem("GameObject/UI/Radial Layout/Auto Radial Layout", priority = 10)]
		static void CreateLayout()
		{
			CreatePrefab(layoutPrefabPath);
		}

		[MenuItem("GameObject/UI/Radial Layout/Radial Menu", priority = 11)]
		static void CreateRadialMenu()
		{
			CreatePrefab(radialMenuPrefabPath);
		}

		[MenuItem("GameObject/UI/Radial Layout/Query System/Assign unique identifiers",priority = 200)] 
		public static void AssignUniqueIds()
		{
			foreach (var layout in GameObject.FindObjectsOfType<RadialLayout>())
				if (!layout.IsSubLayout)
					RadialLayoutQueryManager.AssignUniqueIdentifiers(layout);
		}

		[MenuItem("GameObject/UI/Radial Layout/About", priority = 500)]
		static void OpenAboutDialog()
		{
			RadialLayoutAboutDialog.Init();
		}

		private static void CreatePrefab(string path)
		{
			RadialLayout instance = (RadialLayout)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<RadialLayout>(path), Selection.activeTransform);

			if (instance != null)
			{
				Undo.RegisterCreatedObjectUndo(instance.gameObject, $"Create {instance.name}");
				Selection.activeObject = instance;
			}
			else
				EditorUtility.DisplayDialog("Error", "Couldn't find the prefab to instantiate", "Ok");
		}

	}
}
#endif
