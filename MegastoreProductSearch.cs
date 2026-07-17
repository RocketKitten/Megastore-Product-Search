using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using DFTGames.Localization;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MegastoreProductSearch
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class MegastoreProductSearchPlugin : BaseUnityPlugin
    {
        private const string PluginGuid = "com.syerra.megastore.productsearch";
        private const string PluginName = "Megastore Product Search";
        private const string PluginVersion = "1.0.0";

        private const float ToolbarWidth = 680f;
        private const float ToolbarHeight = 40f;
        private const float ToolbarX = 75f;
        private const float ToolbarY = 145f;

        internal static string SearchText = string.Empty;
        internal static int VisibleProductCount;
        internal static ProductsWindow ActiveProductsWindow;
        internal static ManualLogSource ModLogger;

        private Harmony _harmony;
        private GameObject _nativeToolbar;
        private TMP_InputField _searchInput;
        private Button _clearButton;
        private TextMeshProUGUI _resultLabel;
        private TMP_FontAsset _interfaceFont;
        private bool _isUpdatingInput;

        private void Awake()
        {
            ModLogger = Logger;

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll();

            Logger.LogInfo($"{PluginName} {PluginVersion} loaded successfully!");
        }

        private void Update()
        {
            ProductsWindow window = ActiveProductsWindow;

            bool shouldShow =
                window != null &&
                window.gameObject.activeInHierarchy &&
                window.IsOpen();

            if (shouldShow && _nativeToolbar == null)
            {
                CreateNativeToolbar(window);
            }

            if (_nativeToolbar != null &&
                _nativeToolbar.activeSelf != shouldShow)
            {
                _nativeToolbar.SetActive(shouldShow);
            }

            if (!shouldShow || _nativeToolbar == null)
            {
                return;
            }

            UpdateResultLabel();
            UpdateClearButton();
            SyncSearchInput();
        }

        private void UpdateResultLabel()
        {
            if (_resultLabel == null)
            {
                return;
            }

            string resultText =
                VisibleProductCount == 1
                    ? "1 product"
                    : $"{VisibleProductCount} products";

            if (_resultLabel.text != resultText)
            {
                _resultLabel.text = resultText;
            }
        }

        private void UpdateClearButton()
        {
            if (_clearButton != null)
            {
                _clearButton.interactable =
                    !string.IsNullOrEmpty(SearchText);
            }
        }

        private void SyncSearchInput()
        {
            if (_searchInput == null ||
                _isUpdatingInput ||
                _searchInput.text == SearchText)
            {
                return;
            }

            _isUpdatingInput = true;
            _searchInput.SetTextWithoutNotify(SearchText);
            _isUpdatingInput = false;
        }

        private void CreateNativeToolbar(ProductsWindow window)
        {
            try
            {
                _interfaceFont = FindInterfaceFont(window);

                Transform canvasTransform =
                    FindCanvasTransform(window.transform);

                if (canvasTransform == null)
                {
                    throw new InvalidOperationException(
                        "Could not find the Shopping UI Canvas.");
                }

                RectTransform parentRect =
                    canvasTransform as RectTransform;

                if (parentRect == null)
                {
                    throw new InvalidOperationException(
                        "Shopping UI Canvas has no RectTransform.");
                }

                _nativeToolbar = new GameObject(
                    "ProductSearchToolbar",
                    typeof(RectTransform),
                    typeof(Image));

                _nativeToolbar.transform.SetParent(parentRect, false);

                RectTransform toolbarRect =
                    _nativeToolbar.GetComponent<RectTransform>();

                toolbarRect.anchorMin = new Vector2(0.5f, 1f);
                toolbarRect.anchorMax = new Vector2(0.5f, 1f);
                toolbarRect.pivot = new Vector2(0.5f, 1f);
                toolbarRect.sizeDelta =
                    new Vector2(ToolbarWidth, ToolbarHeight);
                toolbarRect.anchoredPosition =
                    new Vector2(ToolbarX, ToolbarY);

                _nativeToolbar.transform.SetAsLastSibling();

                AddOverlayCanvas();

                Logger.LogInfo(
                    "Native toolbar parented to Canvas object: " +
                    canvasTransform.name);

                Image toolbarBackground =
                    _nativeToolbar.GetComponent<Image>();

                toolbarBackground.color =
                    new Color(0.025f, 0.025f, 0.025f, 0.96f);
                toolbarBackground.raycastTarget = false;

                CreateSearchInput(toolbarRect);
                CreateClearButton(toolbarRect);
                CreateResultLabel(toolbarRect);

                _nativeToolbar.SetActive(true);

                Logger.LogInfo(
                    "Native product search toolbar created.");
            }
            catch (Exception exception)
            {
                Logger.LogError(
                    "Could not create native search toolbar: " +
                    exception);
            }
        }

        private static Transform FindCanvasTransform(
            Transform startTransform)
        {
            Transform currentTransform = startTransform;

            while (currentTransform != null)
            {
                Component[] components =
                    currentTransform.GetComponents<Component>();

                for (int i = 0; i < components.Length; i++)
                {
                    Component component = components[i];

                    if (component != null &&
                        component.GetType().Name == "Canvas")
                    {
                        return currentTransform;
                    }
                }

                currentTransform = currentTransform.parent;
            }

            return null;
        }

        private void AddOverlayCanvas()
        {
            Type canvasType =
                Type.GetType(
                    "UnityEngine.Canvas, UnityEngine.UIModule");

            if (canvasType == null)
            {
                return;
            }

            Component overlayCanvas =
                _nativeToolbar.AddComponent(canvasType);

            PropertyInfo overrideSortingProperty =
                canvasType.GetProperty("overrideSorting");

            PropertyInfo sortingOrderProperty =
                canvasType.GetProperty("sortingOrder");

            overrideSortingProperty?.SetValue(
                overlayCanvas,
                true,
                null);

            sortingOrderProperty?.SetValue(
                overlayCanvas,
                500,
                null);

            if (_nativeToolbar.GetComponent<GraphicRaycaster>() == null)
            {
                _nativeToolbar.AddComponent<GraphicRaycaster>();
            }
        }

        private TMP_FontAsset FindInterfaceFont(
            ProductsWindow window)
        {
            TMP_Text localText =
                window.GetComponentInChildren<TMP_Text>(true);

            if (localText != null &&
                localText.font != null)
            {
                return localText.font;
            }

            TMP_Text[] allText =
                Resources.FindObjectsOfTypeAll<TMP_Text>();

            for (int i = 0; i < allText.Length; i++)
            {
                if (allText[i] != null &&
                    allText[i].font != null)
                {
                    return allText[i].font;
                }
            }

            return null;
        }

        private void CreateSearchInput(
            RectTransform toolbarRect)
        {
            GameObject inputObject = new GameObject(
                "SearchInput",
                typeof(RectTransform),
                typeof(Image),
                typeof(TMP_InputField));

            inputObject.transform.SetParent(toolbarRect, false);

            RectTransform inputRect =
                inputObject.GetComponent<RectTransform>();

            inputRect.anchorMin = new Vector2(0f, 0.5f);
            inputRect.anchorMax = new Vector2(0f, 0.5f);
            inputRect.pivot = new Vector2(0f, 0.5f);
            inputRect.anchoredPosition = new Vector2(5f, 0f);
            inputRect.sizeDelta = new Vector2(430f, 32f);

            Image inputBackground =
                inputObject.GetComponent<Image>();

            inputBackground.color =
                new Color(0.035f, 0.035f, 0.035f, 1f);
            inputBackground.raycastTarget = true;

            TextMeshProUGUI inputText =
                CreateInputText(
                    inputObject.transform,
                    "Text",
                    17f,
                    new Color(0.95f, 0.95f, 0.95f, 1f));

            TextMeshProUGUI placeholder =
                CreateInputText(
                    inputObject.transform,
                    "Placeholder",
                    16f,
                    new Color(0.62f, 0.62f, 0.62f, 1f));

            placeholder.text =
                "Search products...  Try shelf:cool";

            _searchInput =
                inputObject.GetComponent<TMP_InputField>();

            _searchInput.textComponent = inputText;
            _searchInput.placeholder = placeholder;
            _searchInput.textViewport = inputRect;
            _searchInput.lineType =
                TMP_InputField.LineType.SingleLine;
            _searchInput.characterLimit = 80;
            _searchInput.text = SearchText;
            _searchInput.interactable = true;
            _searchInput.enabled = true;
            _searchInput.onValueChanged.AddListener(
                OnSearchValueChanged);

            Button focusButton =
                inputObject.AddComponent<Button>();

            focusButton.transition =
                Selectable.Transition.None;
            focusButton.targetGraphic = inputBackground;
            focusButton.onClick.AddListener(
                FocusSearchInput);
        }

        private TextMeshProUGUI CreateInputText(
            Transform parent,
            string objectName,
            float fontSize,
            Color color)
        {
            GameObject textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(TextMeshProUGUI));

            textObject.transform.SetParent(parent, false);

            RectTransform textRect =
                textObject.GetComponent<RectTransform>();

            StretchTextRect(textRect);

            TextMeshProUGUI text =
                textObject.GetComponent<TextMeshProUGUI>();

            ApplyTextDefaults(text, fontSize, color);

            return text;
        }

        private void CreateClearButton(
            RectTransform toolbarRect)
        {
            GameObject buttonObject = new GameObject(
                "ClearButton",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));

            buttonObject.transform.SetParent(toolbarRect, false);

            RectTransform buttonRect =
                buttonObject.GetComponent<RectTransform>();

            buttonRect.anchorMin = new Vector2(0f, 0.5f);
            buttonRect.anchorMax = new Vector2(0f, 0.5f);
            buttonRect.pivot = new Vector2(0f, 0.5f);
            buttonRect.anchoredPosition =
                new Vector2(445f, 0f);
            buttonRect.sizeDelta =
                new Vector2(78f, 32f);

            Image buttonImage =
                buttonObject.GetComponent<Image>();

            buttonImage.color =
                new Color(0.20f, 0.23f, 0.27f, 1f);

            _clearButton =
                buttonObject.GetComponent<Button>();

            ColorBlock buttonColors =
                _clearButton.colors;

            buttonColors.normalColor =
                new Color(0.20f, 0.23f, 0.27f, 1f);
            buttonColors.highlightedColor =
                new Color(0.29f, 0.34f, 0.40f, 1f);
            buttonColors.pressedColor =
                new Color(0.12f, 0.15f, 0.18f, 1f);
            buttonColors.disabledColor =
                new Color(0.10f, 0.10f, 0.10f, 0.65f);

            _clearButton.colors = buttonColors;
            _clearButton.onClick.AddListener(ClearSearch);

            GameObject labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));

            labelObject.transform.SetParent(
                buttonObject.transform,
                false);

            RectTransform labelRect =
                labelObject.GetComponent<RectTransform>();

            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label =
                labelObject.GetComponent<TextMeshProUGUI>();

            ApplyTextDefaults(label, 16f, Color.white);
            label.alignment = TextAlignmentOptions.Center;
            label.text = "Clear";
        }

        private void CreateResultLabel(
            RectTransform toolbarRect)
        {
            GameObject resultObject = new GameObject(
                "ResultCount",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));

            resultObject.transform.SetParent(toolbarRect, false);

            RectTransform resultRect =
                resultObject.GetComponent<RectTransform>();

            resultRect.anchorMin = new Vector2(0f, 0.5f);
            resultRect.anchorMax = new Vector2(0f, 0.5f);
            resultRect.pivot = new Vector2(0f, 0.5f);
            resultRect.anchoredPosition =
                new Vector2(535f, 0f);
            resultRect.sizeDelta =
                new Vector2(138f, 32f);

            _resultLabel =
                resultObject.GetComponent<TextMeshProUGUI>();

            ApplyTextDefaults(
                _resultLabel,
                16f,
                Color.white);

            _resultLabel.alignment =
                TextAlignmentOptions.MidlineLeft;
            _resultLabel.text =
                $"{VisibleProductCount} products";
        }

        private static void StretchTextRect(
            RectTransform textRect)
        {
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 2f);
            textRect.offsetMax = new Vector2(-8f, -2f);
        }

        private void ApplyTextDefaults(
            TextMeshProUGUI text,
            float fontSize,
            Color color)
        {
            if (_interfaceFont != null)
            {
                text.font = _interfaceFont;
            }

            text.fontSize = fontSize;
            text.color = color;
            text.textWrappingMode =
                TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            text.alignment =
                TextAlignmentOptions.MidlineLeft;
        }

        private void FocusSearchInput()
        {
            if (_searchInput == null)
            {
                return;
            }

            _searchInput.interactable = true;
            _searchInput.enabled = true;
            _searchInput.Select();
            _searchInput.ActivateInputField();

            Logger.LogInfo("Search input focused.");
        }

        private void OnSearchValueChanged(string value)
        {
            if (_isUpdatingInput)
            {
                return;
            }

            SearchText = value;
            RefreshSearch(ActiveProductsWindow);
        }

        private void ClearSearch()
        {
            SearchText = string.Empty;

            if (_searchInput != null)
            {
                _isUpdatingInput = true;
                _searchInput.SetTextWithoutNotify(string.Empty);
                _isUpdatingInput = false;
            }

            RefreshSearch(ActiveProductsWindow);
        }

        private void RefreshSearch(ProductsWindow window)
        {
            if (window == null)
            {
                return;
            }

            try
            {
                window.RefreshProductUIs();
            }
            catch (Exception exception)
            {
                Logger.LogError(
                    "Could not refresh product search: " +
                    exception);
            }
        }

        private void OnDestroy()
        {
            if (_searchInput != null)
            {
                _searchInput.onValueChanged.RemoveListener(
                    OnSearchValueChanged);
            }

            if (_clearButton != null)
            {
                _clearButton.onClick.RemoveListener(
                    ClearSearch);
            }

            if (_nativeToolbar != null)
            {
                Destroy(_nativeToolbar);
            }

            _harmony?.UnpatchSelf();
        }
    }

    [HarmonyPatch(
        typeof(ProductsWindow),
        nameof(ProductsWindow.RefreshProductUIs))]
    internal static class RefreshProductUIsPatch
    {
        private static readonly MethodInfo RefreshNavigationMethod =
            AccessTools.Method(
                typeof(ProductsWindow),
                "RefreshNavigation");

        private static bool MatchesSmartSearch(
            ProductData productData,
            string localizedProductName,
            string search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return true;
            }

            string rawProductName =
                productData.type.ToString();

            string rawShelfType =
                productData.shelfType.ToString();

            string localizedShelfType =
                GetLocalizedOrRaw(rawShelfType);

            string rawProductGroup =
                productData.productGroup.ToString();

            string localizedProductGroup =
                GetLocalizedOrRaw(rawProductGroup);

            int licenseNumber =
                productData.requiredLicense;

            string nameText =
                localizedProductName + " " +
                rawProductName;

            string shelfText =
                localizedShelfType + " " +
                rawShelfType;

            string groupText =
                localizedProductGroup + " " +
                rawProductGroup;

            string licenseText =
                licenseNumber + " " +
                "license " + licenseNumber + " " +
                "license" + licenseNumber + " " +
                "lic " + licenseNumber + " " +
                "l" + licenseNumber;

            string allText =
                nameText + " " +
                shelfText + " " +
                groupText + " " +
                licenseText;

            string[] searchTerms =
                search.Split(
                    new[] { ' ' },
                    StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < searchTerms.Length; i++)
            {
                string term = searchTerms[i];
                string fieldName = string.Empty;
                string fieldValue = term;

                int separatorIndex = term.IndexOf(':');

                if (separatorIndex > 0 &&
                    separatorIndex < term.Length - 1)
                {
                    fieldName =
                        term.Substring(0, separatorIndex);

                    fieldValue =
                        term.Substring(separatorIndex + 1);
                }

                string textToSearch;

                if (fieldName.Equals(
                        "name",
                        StringComparison.OrdinalIgnoreCase))
                {
                    textToSearch = nameText;
                }
                else if (fieldName.Equals(
                             "shelf",
                             StringComparison.OrdinalIgnoreCase))
                {
                    textToSearch = shelfText;
                }
                else if (fieldName.Equals(
                             "group",
                             StringComparison.OrdinalIgnoreCase) ||
                         fieldName.Equals(
                             "department",
                             StringComparison.OrdinalIgnoreCase))
                {
                    textToSearch = groupText;
                }
                else if (fieldName.Equals(
                             "license",
                             StringComparison.OrdinalIgnoreCase) ||
                         fieldName.Equals(
                             "lic",
                             StringComparison.OrdinalIgnoreCase))
                {
                    int requestedLicense;

                    if (!int.TryParse(
                            fieldValue,
                            out requestedLicense) ||
                        requestedLicense != licenseNumber)
                    {
                        return false;
                    }

                    continue;
                }
                else
                {
                    textToSearch = allText;
                    fieldValue = term;
                }

                if (textToSearch.IndexOf(
                        fieldValue,
                        StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static string GetLocalizedOrRaw(
            string localizationKey)
        {
            string localizedValue =
                Locale.GetWord(localizationKey);

            return string.IsNullOrEmpty(localizedValue)
                ? localizationKey
                : localizedValue;
        }

        private static bool Prefix(
            ProductsWindow __instance,
            List<ProductUI> ___productUIs,
            ref ProductGroup ___currentFilter,
            GameObject ___wishlistUI,
            TMP_Dropdown ___inShelfStockFilter,
            TMP_Dropdown ___offShelfStockFilter,
            TMP_Dropdown ___storageTypeFilter,
            List<ShelfType> ___currentShelfTypes,
            List<ProductData> ___currentProducts,
            ref bool ___includeOnlyLabeledProducts)
        {
            try
            {
                MegastoreProductSearchPlugin.ActiveProductsWindow =
                    __instance;

                ___currentProducts.Clear();

                List<ProductType> purchasableProducts =
                    ___currentFilter == ProductGroup.VENDING
                        ? SingletonBehaviour<VendingStockManager>
                            .Instance.PurchasableProducts
                        : SingletonBehaviour<StockManager>
                            .Instance.PurchasableProducts;

                bool shelfTypesAlreadyLoaded =
                    ___currentShelfTypes.Count > 0;

                for (int i = 0;
                     i < purchasableProducts.Count;
                     i++)
                {
                    ProductData productData =
                        SingletonBehaviour<ProductPool>
                            .Instance.GetProductData(
                                purchasableProducts[i]);

                    if (productData == null)
                    {
                        continue;
                    }

                    if (!SingletonBehaviour<ProductPool>
                            .Instance.IsPurchasableByPlayer(
                                productData))
                    {
                        continue;
                    }

                    if (productData.productGroup !=
                        ___currentFilter)
                    {
                        continue;
                    }

                    if (GameManager.isDemo)
                    {
                        if (productData.requiredLicense <= 2 &&
                            productData.productGroup !=
                                ProductGroup.ELECTRONICS &&
                            productData.productGroup !=
                                ProductGroup.MUSIC &&
                            productData.productGroup !=
                                ProductGroup.SPORTS)
                        {
                            ___currentProducts.Add(productData);
                        }

                        continue;
                    }

                    if (!shelfTypesAlreadyLoaded &&
                        !___currentShelfTypes.Contains(
                            productData.shelfType))
                    {
                        ___currentShelfTypes.Add(
                            productData.shelfType);
                    }

                    ___currentProducts.Add(productData);
                }

                if (!shelfTypesAlreadyLoaded)
                {
                    List<TMP_Dropdown.OptionData> options =
                        new List<TMP_Dropdown.OptionData>
                        {
                            new TMP_Dropdown.OptionData(
                                Locale.GetWord("dropdown_all"))
                        };

                    for (int i = 0;
                         i < ___currentShelfTypes.Count;
                         i++)
                    {
                        options.Add(
                            new TMP_Dropdown.OptionData(
                                Locale.GetWord(
                                    ___currentShelfTypes[i]
                                        .ToString())));
                    }

                    ___storageTypeFilter.options = options;
                    ___storageTypeFilter.SetValueWithoutNotify(0);
                }

                ___currentProducts.Sort(
                    delegate (ProductData a, ProductData b)
                    {
                        return a.requiredLicense.CompareTo(
                            b.requiredLicense);
                    });

                string search =
                    MegastoreProductSearchPlugin.SearchText
                        ?.Trim() ?? string.Empty;

                int visibleProductCount = 0;

                for (int i = 0;
                     i < ___currentProducts.Count;
                     i++)
                {
                    ProductData productData =
                        ___currentProducts[i];

                    if (productData.requiredLicense > 999)
                    {
                        continue;
                    }

                    string localizedProductName =
                        Locale.GetWord(
                            productData.type.ToString());

                    if (string.IsNullOrEmpty(
                            localizedProductName))
                    {
                        localizedProductName =
                            productData.type.ToString();
                    }

                    if (!MatchesSmartSearch(
                            productData,
                            localizedProductName,
                            search))
                    {
                        continue;
                    }

                    int shelfStock =
                        SingletonBehaviour<StockManager>
                            .Instance
                            .GetAvailableStockOnShelves(
                                productData.type);

                    int boxStock =
                        SingletonBehaviour<StockManager>
                            .Instance
                            .GetAvailableStockInBoxes(
                                productData.type);

                    int maximumProductCount =
                        productData.GetMaxProductCount();

                    bool matchesShelfStock =
                        (___inShelfStockFilter.value != 1 ||
                         shelfStock != 0) &&
                        (___inShelfStockFilter.value != 3 ||
                         shelfStock <= 0) &&
                        (___inShelfStockFilter.value != 2 ||
                         shelfStock < maximumProductCount);

                    bool matchesBoxStock =
                        (___offShelfStockFilter.value != 1 ||
                         boxStock != 0) &&
                        (___offShelfStockFilter.value != 3 ||
                         boxStock <= 0) &&
                        (___offShelfStockFilter.value != 2 ||
                         boxStock < maximumProductCount);

                    bool matchesStorageType =
                        ___storageTypeFilter.value <= 0 ||
                        productData.shelfType ==
                        ___currentShelfTypes[
                            ___storageTypeFilter.value - 1];

                    bool matchesLabelFilter =
                        !___includeOnlyLabeledProducts ||
                        SingletonBehaviour<LabeledStockManager>
                            .Instance
                            .HasLabeledShelfByProductType(
                                productData.type);

                    if (!matchesShelfStock ||
                        !matchesBoxStock ||
                        !matchesStorageType ||
                        !matchesLabelFilter)
                    {
                        continue;
                    }

                    if (visibleProductCount >=
                        ___productUIs.Count)
                    {
                        break;
                    }

                    ProductUI productUI =
                        ___productUIs[visibleProductCount];

                    productUI.Repaint(productData);
                    productUI.UpdateStocks();
                    productUI.gameObject.SetActive(true);

                    visibleProductCount++;
                }

                for (int i = visibleProductCount;
                     i < ___productUIs.Count;
                     i++)
                {
                    if (___productUIs[i] != null)
                    {
                        ___productUIs[i]
                            .gameObject.SetActive(false);
                    }
                }

                MegastoreProductSearchPlugin.VisibleProductCount =
                    visibleProductCount;

                if (___wishlistUI != null)
                {
                    ___wishlistUI.SetActive(
                        GameManager.isDemo);
                }

                RefreshNavigationMethod?.Invoke(
                    __instance,
                    null);

                return false;
            }
            catch (Exception exception)
            {
                MegastoreProductSearchPlugin.ModLogger
                    ?.LogError(
                        "Product search patch failed: " +
                        exception);

                return true;
            }
        }
    }
}