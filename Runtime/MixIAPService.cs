using System;
using System.Collections.Generic;
using System.Linq;
using LittleBit.Modules.IAppModule.Data;
using LittleBit.Modules.IAppModule.Data.ProductWrappers;
using LittleBit.Modules.IAppModule.Data.Purchases;
using LittleBit.Modules.IAppModule.Services;
using LittleBitGames.Environment.Events;
using MixNameSpace;
using UnityEngine.Scripting;

public class MixIAPService : IIAPService,IIAPRevenueEvent
{
    //ToDo понять что это такое)
    private const string CartType = "Shop";
    private const string Signature = "VVO";
    private const string ItemType = "Offer";
    
    private readonly List<OfferConfig> _offerConfigs;
    private readonly CrossPlatformTangles _crossPlatformTangles;
    private readonly List<string> _boughtProducts;
    private readonly IAPService.ProductCollections _productCollection;
    public event Action<string> OnPurchasingSuccess;
    public event Action<string> OnPurchasingFailed;
    public event Action OnInitializationComplete;
    public bool IsInitialized => MixIap.instance.isInit;

    [Preserve]
    public MixIAPService(List<OfferConfig> offerConfigs, CrossPlatformTangles crossPlatformTangles)
    {
        _offerConfigs = offerConfigs;
        _crossPlatformTangles = crossPlatformTangles;
        _boughtProducts = new List<string>();
        _productCollection = new IAPService.ProductCollections();
    }

    public void Init(MixSDKConfig mixSDKConfig)
    {
        MixIap.instance.Init(mixSDKConfig, (s)=>
        {
            OnInit();
        });
        foreach (var item in mixSDKConfig.mixInput.items)
        {
            var offerConfig = _offerConfigs.FirstOrDefault(o => o.Id == item.itemId);
            if (offerConfig == null)
            {
                throw new Exception("Not inited offerconfig with Id - " + item.itemId);
            }
            _productCollection.AddConfig(offerConfig);
        }
        
        MixIap.instance.SetAction((e) =>
        {
            if (e.itemType == ProductType.Consumable)
            {
                MixIap.instance.FinishPurchase(e);
            }
            else if(e.itemType == ProductType.NonConsumable)
            {
                
                MixIap.instance.GetAllNonConsumable();
            }
            OnPurchasingSuccess?.Invoke(e.itemId);
            //send item
            //MixIap.instance.FinishPurchase(e.purchasedProduct.definition.id);
        });
    }

    private void OnInit()
    {
        //_productCollection.AddUnityIAPProductCollection(Purchaser.Instance.StoreController.products);

        OnInitializationComplete?.Invoke();
    }

    public void Purchase(string id, bool freePurchase = false)
    {
/*#if IAP_DEBUG || UNITY_EDITOR
        var product = (GetProductWrapper(id) as EditorProductWrapper);

        if (product is null) return;
            
        if (!product.Metadata.CanPurchase) return;
            
        product!.Purchase();
        OnPurchasingSuccess?.Invoke(id);
        PurchasingProductSuccess(id);
#else*/

            if (freePurchase)
            {
                OnPurchasingSuccess?.Invoke(id);
                PurchasingProductSuccess(id);
                return;
            }

            MixIap.instance.PurchaseItem(id, (value)=>
            {
                OnPurchasingFailed?.Invoke(value);
            });
//#endif
    }

    public void RestorePurchasedProducts(Action<bool> callback)
    {
        MixIap.instance.AppleRestore(callback);
    }

    public IProductWrapper GetProductWrapper(string id)
    {
        
#if IAP_DEBUG || UNITY_EDITOR
        return GetDebugProductWrapper(id);
#else
            try
            {
                return GetRuntimeProductWrapper(id);
            }
            catch
            {
                Debug.LogError($"Can't create runtime product wrapper with id:{id}");
                return null;
            }
#endif
    }

    private IProductWrapper GetDebugProductWrapper(string id)
    {
        throw new NotImplementedException();
    }

    public event Action<IDataEventEcommerce> OnPurchasingProductSuccess;
    
    private void PurchasingProductSuccess(string productId)
    {
        var product = GetProductWrapper(productId);
        var metadata = product.Metadata;
        var definition = product.Definition;
        var receipt = product.TransactionData.Receipt;

        var data = new DataEventEcommerce(
            metadata.CurrencyCode,
            (double) metadata.LocalizedPrice,
            ItemType, definition.Id,
            CartType, receipt,
            Signature);       
        OnPurchasingProductSuccess?.Invoke(data);
    }
}