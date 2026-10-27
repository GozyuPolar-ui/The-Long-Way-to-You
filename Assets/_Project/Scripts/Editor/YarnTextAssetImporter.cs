using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Prince.Editor
{
    [ScriptedImporter(1, "yarn")]
    public sealed class YarnTextAssetImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext context)
        {
            TextAsset dialogue = new TextAsset(File.ReadAllText(context.assetPath));
            dialogue.name = Path.GetFileNameWithoutExtension(context.assetPath);
            context.AddObjectToAsset("dialogue", dialogue);
            context.SetMainObject(dialogue);
        }
    }
}
