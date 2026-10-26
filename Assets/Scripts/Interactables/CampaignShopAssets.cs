using UnityEngine;

/// <summary>
/// PURPOSE:
/// The original props the T12 Supply Counter is dressed from, referenced
/// (never copied or edited) so they load at runtime and ship in builds:
/// - counter: Assets/Synty/PolygonTown/Prefabs/Props/SM_Prop_ShopCounter_01.prefab
/// - shelf:   Assets/Synty/PolygonTown/Prefabs/Props/SM_Prop_ShopShelf_01.prefab
/// - book:    Assets/Synty/PolygonTown/Prefabs/Props/SM_Prop_BookOpen_01.prefab (the sample catalogue)
/// - crate:   Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Crate_01.prefab
/// - books:   Assets/Synty/PolygonOffice/Prefabs/Props/Desk Props/SM_Prop_Book_Group_02.prefab
/// - solid material (T13): Assets/3D Models/CaseFolder/OpenCaseFolder/Materials/Material_0.001.mat,
///   a URP Lit material that DecorModels clones per colour (white base map) for the decor models.
/// The counter instantiates scoped copies under its own root. Lives at
/// Resources/Campaign/shop_assets.asset (same pattern as Level2SceneAssets).
/// </summary>
[CreateAssetMenu(fileName = "shop_assets", menuName = "TaxVenture/Campaign Shop Assets")]
public sealed class CampaignShopAssets : ScriptableObject
{
    public const string ResourcePath = "Campaign/shop_assets";

    public GameObject counterPrefab;
    public GameObject shelfPrefab;
    public GameObject bookPrefab;
    public GameObject cratePrefab;
    public GameObject booksPrefab;
    public Material solidMaterial;

    public static CampaignShopAssets Load() => Resources.Load<CampaignShopAssets>(ResourcePath);
}
