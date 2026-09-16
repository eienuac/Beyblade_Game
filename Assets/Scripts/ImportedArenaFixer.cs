using UnityEngine;

/// <summary>
/// Dışarıdan indirilen 3D Arena modelini tek tıkla oyuna hazır hale getirir.
/// Dik durma sorununu (X=-90), düşme sorununu (MeshCollider) ve malzeme sorununu otomatik çözer.
/// </summary>
public class ImportedArenaFixer : MonoBehaviour
{
    [Header("Otomatik Kurulum Ayarları")]
    [Tooltip("Arenayı yere yatırmak için X eksenini -90 yapar.")]
    public bool fixRotation = true;
    
    [Tooltip("Düşmeyi engellemek için tüm parçalara Mesh Collider ekler.")]
    public bool addColliders = true;

    [Tooltip("Arenanın altından düşmemesi için (zemin çok inceyse) görünmez kalın zemin ekler.")]
    public bool addSafetyFloor = true;

    [Tooltip("FBX ile gelen istenmeyen kamera ve ışıkları otomatik siler.")]
    public bool removeUnwantedCamerasAndLights = true;

    private void Start()
    {
        FixArena();
    }

    [ContextMenu("Arenayı Şimdi Düzelt!")]
    public void FixArena()
    {
        // 1. DİK DURMA SORUNUNU ÇÖZ (ROTATION -90)
        if (fixRotation)
        {
            // Eğer daha önceden döndürülmemişse yatır
            if (Mathf.Abs(transform.rotation.eulerAngles.x) < 1f || Mathf.Abs(transform.rotation.eulerAngles.x - 270f) > 5f)
            {
                transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            }
        }

        // 2. İÇİNDEN DÜŞMEYİ ENGELLE (MESH COLLIDER)
        if (addColliders)
        {
            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter mf in meshFilters)
            {
                if (mf.gameObject.GetComponent<MeshCollider>() == null && mf.gameObject.GetComponent<Collider>() == null)
                {
                    MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                    
                    // Fizik materyali ekle ki sürtünme az olsun
                    PhysicsMaterial arenaMat = new PhysicsMaterial("ArenaPhysics");
                    arenaMat.bounciness = 0.1f;
                    arenaMat.dynamicFriction = 0.1f;
                    arenaMat.staticFriction = 0.1f;
                    mc.material = arenaMat;
                }
            }
        }

        // 3. GÜVENLİK ZEMİNİ (Altı boşsa direkt düşüyorsa)
        if (addSafetyFloor)
        {
            // Zemin objesini bağımsız olarak sahnenin merkezine (0, -0.5, 0) ekliyoruz
            GameObject existingFloor = GameObject.Find("GlobalSafetyFloor");
            if (existingFloor == null)
            {
                GameObject floorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floorObj.name = "GlobalSafetyFloor";
                
                // Hiçbir şeyin child'ı yapmıyoruz ki modelin Scale'inden etkilenmesin!
                floorObj.transform.position = new Vector3(0, -0.7f, 0); 
                floorObj.transform.rotation = Quaternion.identity;
                floorObj.transform.localScale = new Vector3(200f, 1f, 200f); // Çok büyük ve 1 metre kalınlığında!
                
                floorObj.GetComponent<MeshRenderer>().enabled = false; // Görünmez kalsın
                
                // Sürtünme materyali
                PhysicsMaterial floorMat = new PhysicsMaterial("FloorPhysics");
                floorMat.bounciness = 0.1f;
                floorMat.dynamicFriction = 0.5f;
                floorMat.staticFriction = 0.5f;
                floorObj.GetComponent<BoxCollider>().material = floorMat;
            }
        }

        // 4. İSTENMEYEN KAMERA VE IŞIKLARI SİL (Blender/Maya'dan gelen kameralar oyunu bozar)
        if (removeUnwantedCamerasAndLights)
        {
            Camera[] cameras = GetComponentsInChildren<Camera>();
            foreach (Camera cam in cameras)
            {
                Debug.Log("İstenmeyen arena kamerası silindi: " + cam.gameObject.name);
                DestroyImmediate(cam.gameObject);
            }

            Light[] lights = GetComponentsInChildren<Light>();
            foreach (Light l in lights)
            {
                Debug.Log("İstenmeyen arena ışığı silindi: " + l.gameObject.name);
                DestroyImmediate(l.gameObject);
            }
        }
        
        Debug.Log("Arena başarıyla düzeltildi! Rotation = -90, Collider'lar eklendi, Kameralar silindi.");
    }
}
