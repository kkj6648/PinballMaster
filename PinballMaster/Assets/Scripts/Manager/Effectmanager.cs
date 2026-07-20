using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }


    public void SpawnDamageEffect(Vector3 position, int damage, bool iscritical)
    {
        object[] obj = new object[] { damage, iscritical };

        SpawnEffect(position, Object_Type.Effect_Damage, obj);
    }

    public void SpawnHitEffect(Vector3 position)
    {
        SpawnEffect(position, Object_Type.Effect_Hit);
    }

    public void SpawnBoomEffect(Vector3 position)

    {
        SpawnEffect(position, Object_Type.Effect_Boom);
    }

    public void SpawnLaserEffect(Vector3 position ,float value)
    {

        object[] obj = new object[] { value };

        SpawnEffect(position, Object_Type.Effect_Laser, obj);
    }


    public void SpawnDustEffect(Vector3 position)
    {
        SpawnEffect(position, Object_Type.Effect_Dust);
    }

    public void SpawnStoneEffect(Vector3 position)
    {
        SpawnEffect(position, Object_Type.Effect_Stone);
    }

    public void SpawnBloodEffect(Vector3 position)
    {
        SpawnEffect(position, Object_Type.Effect_Blood);
    }

    public void SpawnBossMagicCircleEffect(Vector3 position)
    {
        SpawnEffect(position, Object_Type.Effect_BossCircle);
    }

    public void SpawnHealEffect(Vector3 position)
    {
        SpawnEffect(position, Object_Type.Effect_Heal);
    }



    public FireEffect SpawnFireEffect(Transform target)
    {
        if (target == null)
            return null;

  
        GameObject effectObject = PoolManager.Instance.Get_Object(Object_Type.Effect_Fire);


        if (effectObject == null)
            return null;

        FireEffect effect = effectObject.GetComponent<FireEffect>();


        if (effect == null)
        {
            PoolManager.Instance.Return_Object(Object_Type.Effect_Fire, effectObject);

            return null;
        }

        effect.Init(target);

        return effect;
    }

    public IceEffect SpawnIceEffect(Transform target)
    {
        if (target == null)
            return null;

        if (PoolManager.Instance == null)
            return null;

        GameObject effectObject = PoolManager.Instance.Get_Object(Object_Type.Effect_Ice);


        if (effectObject == null)
            return null;

        IceEffect effect = effectObject.GetComponent<IceEffect>();


        if (effect == null)
        {
            PoolManager.Instance.Return_Object(Object_Type.Effect_Ice, effectObject);
            return null;
        }

        effect.Init(target);

        return effect;
    }

    public void SpawnEffect(Vector3 position, Object_Type type , object[] obj = null)
    {
        if (PoolManager.Instance == null)
        {
            return;
        }

        GameObject effectObject = PoolManager.Instance.Get_Object(type);

        if (effectObject == null)
        {
            return;
        }

        bool initialized = Init_Effect(effectObject, position, type, obj);


        if (initialized == true)
            return;

        PoolManager.Instance.Return_Object(type, effectObject);

    }


    private bool Init_Effect(GameObject effectObject, Vector3 position, Object_Type type , object[] obj = null)
    {
        switch (type)
        {
            case Object_Type.Effect_Damage:
                {
                    DamageEffect effect = effectObject.GetComponent<DamageEffect>();
                    if (effect == null)
                        return false;


                    int damage = (int)obj[0];
                    bool isCritical = (bool)obj[1];

                    effect.Init(damage,position, isCritical);

                    return true;
                }
            case Object_Type.Effect_Hit:
                {
                    HitEffect effect = effectObject.GetComponent<HitEffect>();
                    if (effect == null)
                        return false;

                    effect.Init(position);
                    return true;
                }

            case Object_Type.Effect_Boom:
                {
                    BoomEffect effect = effectObject.GetComponent<BoomEffect>();


                    if (effect == null)
                        return false;

                    effect.Init(position);
                    return true;
                }

            case Object_Type.Effect_Laser:
                {
                    LaserEffect effect = effectObject.GetComponent<LaserEffect>();

                    if (effect == null)
                        return false;

                    float value = (float)obj[0];

                    effect.Init(value);
                    return true;
                }
            case Object_Type.Effect_Dust:
                {
                    DustParticle effect = effectObject.GetComponent<DustParticle>();


                    if (effect == null)
                        return false;

                    effect.Init(position);
                    return true;
                }
            case Object_Type.Effect_Stone:
                {
                    StoneEffect effect = effectObject.GetComponent<StoneEffect>();


                    if (effect == null)
                        return false;

                    effect.Init(position);
                    return true;
                }
            case Object_Type.Effect_Blood:
                {
                    BloodEffect effect = effectObject.GetComponent< BloodEffect>();

                    if (effect == null)
                        return false;

                    effect.Init(position);
                    return true;
                }
            case Object_Type.Effect_BossCircle:
                {
                    BossMagiCircleEffect effect = effectObject.GetComponent<BossMagiCircleEffect>();

                    if (effect == null)
                        return false;

                    effect.Init(position);
                    return true;
                }
            case Object_Type.Effect_Heal:
                {
                    HealParticle effect = effectObject.GetComponent<HealParticle>();

                    if (effect == null)
                        return false;

                    effect.Init(position);
                    return true;
                }

            default:
                {
                    return false;
                }
        }
    }


    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }


}
