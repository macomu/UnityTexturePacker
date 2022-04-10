using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace macomu
{

/// <summary>
/// The status of item (Add, Update and Remove)
/// </summary>
enum TexturePackerListItemStatus
{
    Stay = 0,
    /// <summary>
    /// The item which will be added to sprite.
    /// </summary>
    Add,
    /// <summary>
    /// The item which will be updated.
    /// </summary>
    Update,
    /// <summary>
    /// The item which will be removed in list.
    /// </summary>
    Remove,
}

class TexturePackerListItem
{
    public string Name { get; set; }
    public string AssetPath { get; set; }
    public TexturePackerListItemStatus Status { get; set; }
    public SpriteMetaData MetaData { get; set; }

    public TexturePackerListItem Copy()
    {
        var newItem = new TexturePackerListItem();
        newItem.Name = Name;
        newItem.AssetPath = AssetPath;
        newItem.Status = Status;
        newItem.MetaData = MetaData;
        return newItem;
    }
}

/// <summary>
/// The class which display TexturePacker window.
/// </summary>
public class TexturePackerWindow : EditorWindow
{
    // ------- Constants ----------
    private const string KEY_INDEX = "index";
    // ------- Menu Items -----------
    [MenuItem("Window/ma_comu/TexturePacker")]
    public static void CreateTexturePackerWindow()
    {
        var window = EditorWindow.GetWindow<TexturePackerWindow>();
        window.titleContent = new GUIContent("TexturePacker");
    }

    // ------- Instance Variables ------
    private List<TexturePackerListItem> m_selectedObjects = new List<TexturePackerListItem>();
    // the list which include ObjectField "Atlas" sprites
    private List<TexturePackerListItem> m_itemsIncludedInAtlas = new List<TexturePackerListItem>();
    // the list for displayed items by ListView
    private List<TexturePackerListItem> m_itemsDisplayedListView = new List<TexturePackerListItem>();
    private ListView m_listView = null;
    private VisualElement m_uiRoot = null;
    private VisualTreeAsset m_listItemTreeAsset = null;
    private ObjectField m_atlasField = null;
    private Texture2D m_atlas = null;

    // ------- Instance Methods ------
    /// <summary>
    /// Create GUI for EditorWindow.
    /// </summary>
    public void CreateGUI()
    {
        m_listItemTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(GetResourcePath("TexturePackerSpriteListItem.uxml"));

        VisualTreeAsset uiAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(GetResourcePath("TexturePackerWindow.uxml"));
        m_uiRoot = uiAsset.CloneTree();
        rootVisualElement.Add(m_uiRoot);

        // setup ObjectField for Atlas(Packed texture)
        m_atlasField = m_uiRoot.Q<ObjectField>("AtlasSelector");
        m_atlasField.objectType = typeof(Texture2D);

        m_atlasField.RegisterValueChangedCallback((item) => {
            ChangeSelectAtlas(item.newValue);
        });
        Selection.selectionChanged = ChangeSelect;

        // setup list view
        var listView = m_uiRoot.Q<ListView>("SpriteList");
        listView.makeItem = () => {
            var item = m_listItemTreeAsset.CloneTree();
            var stayButton = item.Q<Button>("SpriteStatusText_Stay");
            var removeButton = item.Q<Button>("SpriteStatusText_Remove");
            System.Action<TexturePackerListItemStatus> setStatus = (TexturePackerListItemStatus status) => {
                var index = GetListViewItemUserData<int>(item, KEY_INDEX);
                var displayedItem = m_itemsDisplayedListView[index];
                displayedItem.Status = status;
                ChangeListItemStatus(item, status);
                var itemInAtlas = m_itemsIncludedInAtlas.FirstOrDefault(i => i.Name == displayedItem.Name);
                if (itemInAtlas != null)
                {
                    itemInAtlas.Status = status;
                }
            };
            stayButton.clicked += () => {
                setStatus(TexturePackerListItemStatus.Remove);
            };
            removeButton.clicked += () => {
                setStatus(TexturePackerListItemStatus.Stay);
            };
            return item;
        };
        listView.bindItem = (element, index) => {
            var targetItem = m_itemsDisplayedListView[index];
            var nameLabel = element.Q<Label>("SpriteNameText");
            nameLabel.text = targetItem.Name;
            ChangeListItemStatus(element, targetItem.Status);
            SetListViewItemUserData(element, KEY_INDEX, index);
        };
        listView.itemsSource = m_itemsDisplayedListView;

#if !UNITY_2021_1_OR_NEWER
        listView.style.minHeight = 50f;
        listView.style.height = 700f;
#endif
        m_listView = listView;

        // setup create/update button
        var updateDisplay = m_uiRoot.Q<VisualElement>("UpdateDisplay");
        var createDisplay = m_uiRoot.Q<VisualElement>("CreateDisplay");
        updateDisplay.style.display = DisplayStyle.None;
        createDisplay.style.display = DisplayStyle.None;

        // setup button callbacks
        var selectPathButton = m_uiRoot.Q<Button>("SelectPathToNewAtlasButton");
        selectPathButton.clicked += OnClickSavePathSelect;
        var createButton = m_uiRoot.Q<Button>("CreateAtlasButton");
        createButton.clicked += OnClickCreateButton;
        var updateButton = m_uiRoot.Q<Button>("UpdateAtlasButton");
        updateButton.clicked += OnClickUpdateButton;
    }

    private void SetListViewItemUserData(VisualElement targetElement, string key, object value)
    {
        var userData = targetElement.userData as Dictionary<string, object>;
        if (userData == null)
        {
            userData = new Dictionary<string, object>();
            targetElement.userData = userData;
        }
        userData[key] = value;
    }

    private T GetListViewItemUserData<T>(VisualElement targetElement, string key)
    {
        var userData = targetElement.userData as Dictionary<string, object>;
        if (userData == null)
        {
            return default(T);
        }
        return (T)userData[key];
    }

    private void ChangeListItemStatus(VisualElement parent, TexturePackerListItemStatus targetStatus)
    {
        var statusDisplays = System.Enum.GetValues(typeof(TexturePackerListItemStatus));
        foreach (var status in statusDisplays.Cast<TexturePackerListItemStatus>())
        {
            var statusName = System.Enum.GetName(typeof(TexturePackerListItemStatus), status);
            var statusButton = parent.Q<Button>($"SpriteStatusText_{statusName}");
            statusButton.style.display = status == targetStatus ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private void ChangeSelect()
    {
        m_itemsDisplayedListView.Clear();
        m_itemsDisplayedListView.AddRange(m_itemsIncludedInAtlas);
        foreach (var selectedTexture in Selection.objects.OfType<Texture2D>())
        {
            var alreadyIncludedIndex = m_itemsDisplayedListView.FindIndex((item) => item.Name == selectedTexture.name);
            var newRect = new Rect(0, 0, selectedTexture.width, selectedTexture.height);
            if (alreadyIncludedIndex >= 0)
            {
                // Will update texture
                var newItem = m_itemsDisplayedListView[alreadyIncludedIndex].Copy();
                var spriteMetaData = newItem.MetaData;
                spriteMetaData.rect = newRect;
                newItem.Status = TexturePackerListItemStatus.Update;
                newItem.AssetPath = AssetDatabase.GetAssetPath(selectedTexture);
                newItem.MetaData = spriteMetaData;

                m_itemsDisplayedListView[alreadyIncludedIndex] = newItem;
            } else {
                // Will add texture
                var spriteMetaData = new SpriteMetaData();
                spriteMetaData.rect = newRect;
                var newItem = new TexturePackerListItem();
                newItem.AssetPath = AssetDatabase.GetAssetPath(selectedTexture);
                newItem.Status = TexturePackerListItemStatus.Add;
                newItem.Name = selectedTexture.name;
                newItem.MetaData = spriteMetaData;
                m_itemsDisplayedListView.Add(newItem);
            }
        }

        UpdateDisplay();
    }

    private void ChangeSelectAtlas(Object atlasObject)
    {
        m_atlas = atlasObject as Texture2D;
        m_itemsIncludedInAtlas.Clear();
        m_itemsDisplayedListView.Clear();
        if (m_atlas == null)
        {
            UpdateDisplay();
            return;
        }
        var atlasPath = AssetDatabase.GetAssetPath(m_atlas);
        var textureImporter = TextureImporter.GetAtPath(atlasPath) as TextureImporter;
        if (textureImporter == null)
        {
            Debug.LogWarning("Faild to get texture importer.");
            UpdateDisplay();
            return;
        }

        var spriteMetaData = textureImporter.spritesheet;
        foreach (var meta in spriteMetaData)
        {
            TexturePackerListItem item = new TexturePackerListItem();
            item.AssetPath = atlasPath;
            item.Name = meta.name;
            item.Status = TexturePackerListItemStatus.Stay;
            item.MetaData = meta;

            m_itemsIncludedInAtlas.Add(item);
        }

        // 一度クリアする
        m_itemsDisplayedListView.AddRange(m_itemsIncludedInAtlas);
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        // Update list
#if UNITY_2021_1_OR_NEWER
        m_listView.Rebuild();
#else
        m_listView.Refresh();
#endif
        // Update create/update button
        var updateDisplay = m_uiRoot.Q<VisualElement>("UpdateDisplay");
        var createDisplay = m_uiRoot.Q<VisualElement>("CreateDisplay");
        var willCreate = m_atlas == null;
        updateDisplay.style.display = willCreate ? DisplayStyle.None : DisplayStyle.Flex;
        createDisplay.style.display = willCreate ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void Pack()
    {
        // Prepare add/update texture source
        foreach (var item in m_itemsDisplayedListView.Where(item => item.Status == TexturePackerListItemStatus.Add || item.Status == TexturePackerListItemStatus.Update))
        {
            var importer = TextureImporter.GetAtPath(item.AssetPath) as TextureImporter;
            importer.textureType = TextureImporterType.Default;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Point;
            importer.isReadable = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }

        if (m_atlas != null)
        {
            var atlasPath = AssetDatabase.GetAssetPath(m_atlas);
            var atlasImporter = TextureImporter.GetAtPath(atlasPath) as TextureImporter;
            atlasImporter.isReadable = true;
            atlasImporter.filterMode = FilterMode.Point;
            atlasImporter.npotScale = TextureImporterNPOTScale.None;
            atlasImporter.SaveAndReimport();
            m_atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
        }

        AssetDatabase.Refresh();

        var packTargetTextures = m_itemsDisplayedListView.Select(item => {
            switch (item.Status)
            {
            case TexturePackerListItemStatus.Stay:
                // Do after switch
                break;
            case TexturePackerListItemStatus.Add:
            case TexturePackerListItemStatus.Update:
                return AssetDatabase.LoadAssetAtPath<Texture2D>(item.AssetPath);
            case TexturePackerListItemStatus.Remove:
                return null;
            default:
                return null;
            }
            var rect = item.MetaData.rect;
            
            var texture = new Texture2D((int)rect.size.x, (int)rect.size.y);
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, m_atlas.GetPixel((int)rect.xMin + x, (int)rect.yMin + y));
                }
            }
            texture.name = item.Name;
            texture.Apply();
            return texture;
        })
            .Where(t => t != null)
            .ToArray();
        var paddingInput = m_uiRoot.Q<IntegerField>("InputPadding");
        var padding = paddingInput == null ? 2 : paddingInput.value;
        var maxSizeInput = m_uiRoot.Q<IntegerField>("InputTextureMaxSize");
        var maxSize = maxSizeInput == null ? 2048 : maxSizeInput.value;
        var texturePacker = new UnityTexturePacker();
        var (packedRects, packedTexture) = texturePacker.Pack(packTargetTextures, padding, maxSize);
        if (packedRects == null)
        {
            throw new System.Exception("Packing textures is failed.");
        }
        var spriteMetaDataArray = new SpriteMetaData[packedRects.Length];
        for (int i = 0; i < packedRects.Length; i++)
        {
            var spriteName = packTargetTextures[i].name;
            var existsData = m_itemsIncludedInAtlas.FirstOrDefault(item => item.Name == spriteName);
            var spriteMetaData = existsData == null ? new SpriteMetaData() : existsData.MetaData;
            spriteMetaData.name = spriteName;
            var rect = packedRects[i];
            spriteMetaData.rect = new Rect(
                new Vector2(rect.position.x * packedTexture.width, rect.position.y * packedTexture.height),
                new Vector2(rect.size.x * packedTexture.width, rect.size.y * packedTexture.height)
            );
            spriteMetaDataArray[i] = spriteMetaData;
        }
        string savePath = null;
        if (m_atlas == null)
        {
            var pathTextField = m_uiRoot.Q<TextField>("CreateNameField");
            savePath = pathTextField.value;
            var projectDir = Directory.GetCurrentDirectory();
            savePath = savePath.Replace($"{projectDir}/", string.Empty);
        } else {
            savePath = AssetDatabase.GetAssetPath(m_atlas);
        }
        savePath = savePath.StartsWith("Assets/") ? savePath : $"Assets/{savePath}";
        savePath = savePath.EndsWith(".png") ? savePath : $"Assets/{savePath}";
        File.WriteAllBytes(savePath, packedTexture.EncodeToPNG());
        AssetDatabase.ImportAsset(savePath);

        AssetDatabase.Refresh();
        // save SpriteDataMeta
        var packedTextureImporter = TextureImporter.GetAtPath(savePath) as TextureImporter;
        packedTextureImporter.textureType = TextureImporterType.Sprite;
        packedTextureImporter.spriteImportMode = SpriteImportMode.Multiple;
        packedTextureImporter.spritesheet = spriteMetaDataArray;
        EditorUtility.SetDirty(packedTextureImporter);
        packedTextureImporter.SaveAndReimport();
        m_atlasField.value = AssetDatabase.LoadAssetAtPath<Texture2D>(savePath);
        ChangeSelectAtlas(m_atlasField.value);
    }

    private void OnClickCreateButton()
    {
        try
        {
            Pack();
        } catch(System.Exception e) {
            ShowError(e.Message);
        }
    }

    private void OnClickUpdateButton()
    {
        try
        {
            Pack();
        } catch(System.Exception e) {
            ShowError(e.Message);
        }
    }

    private void OnClickSavePathSelect()
    {
        var path = EditorUtility.SaveFilePanel("Select save path", "Assets", "atlas.png", ".png");
        var pathTextField = m_uiRoot.Q<TextField>("CreateNameField");
        pathTextField.value = path;
    }

    private static string GetResourcePath(string fileName)
    {
        return Path.Combine("Assets/ma_comu/Editor/TexturePacker/UIResources", fileName);
    }

    private void ShowError(string detail)
    {
        EditorUtility.DisplayDialog("Error", detail, "Close");
    }
}

}