﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : SingletonBehaviour<Player>
{
    //public IntVariable durabilityI;
    public FloatVariable durability;
    public float Durability
    {
        get
        {
            return durability.value;
        }
        set
        {
            durability.value = value;
            if (value > 100)
                durability.value = 100.0f;
            if (durability.value <= 0)
            {
                KillPlayer();
            }
        }
    }
    public Inventory inventory;
    [Header("Body Parts")]
    public EquippedBodyPart equippedBodyPart = null;
    [SerializeField]
    private int[] _upgradeLevels = new int[6];

    public int GetUpgradeLevel(BodyPartType bodyPartType)
    {
        return _upgradeLevels[(int)bodyPartType];
    }

    #region Player Stat
    private int[] _raceAffinity = new int[6];
    public int GetRaceAffinity(Race race)
    {
        if (_raceAffinity[(int)race] == 0)
            return 1;
        return _raceAffinity[(int)race];
    }
    [Header("Status")]
    public Status toolStat;
    [SerializeField]
    private Status _bodyPartStat = null;

    private bool _darkMagic = false;
    public bool DarkMagic
    {
        get
        {
            return _bodyPartStat.darkMagic || toolStat.darkMagic;
        }
        set
        {
            _bodyPartStat.darkMagic = value;
            UpdateAllBodyStat(_bodyPartStat, equippedBodyPart.bodyParts);
        }
    }

    public bool Magic
    {
        get
        {
            if (DarkMagic)
                return true;
            return _bodyPartStat.magic ||  toolStat.magic;
        }
    }

    public bool NightVision
    {
        get
        {
            return _bodyPartStat.nightVision || toolStat.nightVision;
        }
    }

    public void ResetBodyAffinity()
    {
        _raceAffinity[(int)Race.All] = 1;
        for(int i=1;i<_raceAffinity.Length;i++)
        {
            _raceAffinity[i] = 0;
        }
    }
    public int Atk
    {
        get
        {
            return toolStat.atk + _bodyPartStat.atk + GameManager.Inst.research.GetStatBonus(Status.StatName.Atk);
        }
    }
    public int Def
    {
        get
        {
            return toolStat.def + _bodyPartStat.def + GameManager.Inst.research.GetStatBonus(Status.StatName.Def);
        }
    }
    public int Dex
    {
        get
        {
            return toolStat.dex + _bodyPartStat.dex + GameManager.Inst.research.GetStatBonus(Status.StatName.Dex);
        }
    }
    public int Mana
    {
        get
        {
            if(_bodyPartStat.darkMagic)
                return toolStat.mana + _bodyPartStat.mana + GameManager.Inst.research.GetStatBonus(Status.StatName.Mana) + 500;
            return toolStat.mana + _bodyPartStat.mana + GameManager.Inst.research.GetStatBonus(Status.StatName.Atk);
        }
    }
    public int Endurance
    {
        get
        {
            return toolStat.endurance + _bodyPartStat.endurance + GameManager.Inst.research.GetStatBonus(Status.StatName.Endurance);
        }
    }

    public int GetStatus(Status.StatName statName)
    {
        switch(statName)
        {
            case Status.StatName.Atk:
                return Atk;
            case Status.StatName.Def:
                return Def;
            case Status.StatName.Dex:
                return Dex;
            case Status.StatName.Mana:
                return Mana;
            case Status.StatName.Endurance:
                return Endurance;
            default:
                return -1;
        }
    }
    #endregion

    public void InitPlayer()
    {
        //BodyRegenerationRate = 0;
        UpdateAllPlayerBodyStatus(_raceAffinity, equippedBodyPart.bodyParts, _bodyPartStat);
        UpdateAllPlayerSprites();
    }
    
    public void KillPlayer()
    {
        Debug.Log("Game Over");
        GeneralUIManager.Inst.NoticeGameOver();
    }

    public void HealPlayer()
    {
        Durability = 100.0f;
        GeneralUIManager.Inst.UpdateTextDurability();
    }

    #region Body decay

    private int decayRateExploration = 5;
    private int decayRateHome = 3;
    public float BodyRegenerationRate 
    {
        get
        {
            return GameManager.Inst.research.GetRegenBonus();
        }
    }

    private float BodyDecayRate
    {
        get
        {
            if(GameManager.Inst.IsHome)
            {
                if (HomeUIManager.Inst.panelAssemble.activeSelf == true)
                {
                    Debug.Log("BodyDecayRate: 0");
                    return 0;
                }
                else
                {
                    Debug.Log("decayRateHome - BodyRegenerationRate");
                    return decayRateHome - BodyRegenerationRate;
                }
            }
            else
            {
                Debug.Log("decayRateExploration - BodyRegenerationRate");
                return decayRateExploration - BodyRegenerationRate;
            }
        }
    }
    
    /// <summary>
    /// turn만큼 신체의 부패를 진행한다.
    /// </summary>
    /// <param name="turn"></param>
    public void DecayBody(int turn)
    {
        Durability = durability.value - BodyDecayRate * turn;
    }


    #endregion

    #region Change Player Body Methods

    /// <summary>
    /// 플레이어의 신체를 교환한다.
    /// </summary>
    /// <param name="bodyPart">플레이어가 장착할 BodyPart</param>
    /// <param name="chestIndex">Player가 장착할 BodyPart의 chest Index</param>
    /// <returns>플레이어가 장착하고 있던 BodyPart</returns>
    public void ExchangePlayerBody(BodyPart bodyPart, int chestIndex)
    {
        BodyPart returnedBodyPart;
        returnedBodyPart = ExchangePlayerBodyObject(equippedBodyPart.bodyParts, bodyPart);
        StorageManager.Inst.DeleteFromChest(chestIndex);
        //if(!StorageManager.Inst.AddItemToChest(returnedBodyPart))
        //{
        //    ExchangePlayerBodyObject(_equippedBodyPart.bodyParts, returnedBodyPart);
        //    Debug.Log("신체 교환 실패, 창고에 아이템을 추가할 수 없습니다.");
        //    return;
        //}
        ChangeAllPlayerBodyStatus(_bodyPartStat, _raceAffinity, bodyPart, returnedBodyPart);
        if (returnedBodyPart.race == Race.Machine)
        {
            ResetUpgrade(returnedBodyPart);
        }
        ChangePlayerBodyPartSprite(bodyPart.bodyPartType);

        #region legacy
        //switch (bodyPart.bodyPartType)
        //{
        //    case BodyPartType.Head:
        //        equipping = equippedBodyPart.Head;
        //        equippedBodyPart.Head = bodyPart;
        //        transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>().sprite = head.bodyPartSprite;
        //        break;
        //    case BodyPartType.Body:
        //        equipping = body;
        //        body = bodyPart;
        //        equippedBodyPart.Body = bodyPart;
        //        transform.GetChild(1).gameObject.GetComponent<SpriteRenderer>().sprite = body.bodyPartSprite;
        //        break;
        //    case BodyPartType.LeftArm:
        //        equipping = leftArm;
        //        leftArm = bodyPart;
        //        equippedBodyPart.LeftArm = bodyPart;
        //        transform.GetChild(2).gameObject.GetComponent<SpriteRenderer>().sprite = leftArm.bodyPartSprite;
        //        break;
        //    case BodyPartType.RightArm:
        //        equipping = rightArm;
        //        rightArm = bodyPart;
        //        equippedBodyPart.RightArm = bodyPart;
        //        transform.GetChild(3).gameObject.GetComponent<SpriteRenderer>().sprite = rightArm.bodyPartSprite;
        //        break;
        //    case BodyPartType.LeftLeg:
        //        equipping = leftLeg;
        //        leftLeg = bodyPart;
        //        equippedBodyPart.LeftLeg = bodyPart;
        //        transform.GetChild(4).gameObject.GetComponent<SpriteRenderer>().sprite = leftLeg.bodyPartSprite;
        //        break;
        //    case BodyPartType.RightLeg:
        //        equipping = rightLeg;
        //        rightLeg = bodyPart;
        //        equippedBodyPart.RightLeg = bodyPart;
        //        transform.GetChild(5).gameObject.GetComponent<SpriteRenderer>().sprite = rightLeg.bodyPartSprite;
        //        break;
        //    default:
        //        Debug.Log("wrong item Type");
        //        return null;
        //}
        #endregion

        HomeUIManager.Inst.panelNotice.SetActive(true);
        HomeUIManager.Inst.textNotice.text = bodyPart.energyPotential.ToString() + " 에너지를 소모하여\n"
            + bodyPart.itemName + "\n아이템을 장착하였습니다.";
        Debug.Log("장착된 신체 : " + bodyPart.itemName + "\n탈착된 신체 : " + returnedBodyPart.itemName);

        return;
    }

    private BodyPart ExchangePlayerBodyObject(BodyPart[] equippedBodyParts, BodyPart bodyPart)
    {
        for (int i = 0; i < 6; i++)
        {
            if(equippedBodyParts[i] != null)
                Debug.Log(equippedBodyParts[i].itemName);
        }
        BodyPart equipping = equippedBodyParts[(int)bodyPart.bodyPartType];
        if(equipping != null)
            GameManager.Inst.bodyDisassembly.GetBonusItem(equipping);
        equippedBodyParts[(int)bodyPart.bodyPartType] = bodyPart;

        return equipping;
    }

    /// <summary>
    /// 장착하고 있던 신체가 기계라면, 해당 부위의 강화도를 초기화한다.
    /// </summary>
    /// <param name="equipping"></param>
    private void ResetUpgrade(BodyPart equipping)
    {
        _upgradeLevels[(int)equipping.bodyPartType] = 0;
    }

    /// <summary>
    /// bodyPartType에 해당하는 플레이어 신체 스프라이트를 업데이트한다.
    /// </summary>
    private void ChangePlayerBodyPartSprite(BodyPartType bodyPartType)
    {
        if (equippedBodyPart.bodyParts[(int)bodyPartType] != null)
            transform.GetChild((int)bodyPartType).gameObject.GetComponent<SpriteRenderer>().sprite = equippedBodyPart.bodyParts[(int)bodyPartType].bodyPartSprite;
        else
            transform.GetChild((int)bodyPartType).gameObject.GetComponent<SpriteRenderer>().sprite = null;
    }

    /// <summary>
    /// playerBodyPart와 returnedBodyPart의 신체교환에 대한 신체 스텟 변화를 적용한다. 
    /// </summary>
    /// <param name="playerBodyPart">플레이어가 장착할 BodyPart</param>
    private void ChangeAllPlayerBodyStatus(Status bodyPartStat, int[] raceAffinity, BodyPart playerBodyPart, BodyPart returnedBodyPart)
    {
        UpdateBodyStat(bodyPartStat, playerBodyPart, returnedBodyPart);
        UpdateBodyAffinity(raceAffinity, playerBodyPart, returnedBodyPart);
    }

    #endregion

    #region Update Player Methods

    /// <summary>
    /// 모든 신체의 Sprite를 Update한다.
    /// </summary>
    private void UpdateAllPlayerSprites()
    {
        for(int indexBody = 0; indexBody < equippedBodyPart.bodyParts.Length; indexBody++)
        {
            ChangePlayerBodyPartSprite((BodyPartType)indexBody);
        }
    }

    // Player의 신체에 해당하는 스텟과 종족동화율을 업데이트한다.
    // StorageManager의 inventory status methods region을 참고
    /// <summary>
    /// Player의 모든 신체를 참조하여 bodyPartStatus와 raceAffinity를 Update한다.
    /// </summary>
    public void UpdateAllPlayerBodyStatus(int[] raceAffinity, BodyPart[] playerBodyParts, Status bodyPartStatus)
    {
        UpdateAllBodyAffinity(raceAffinity, playerBodyParts);
        UpdateAllBodyStat(bodyPartStatus, playerBodyParts);
    }

    /// <summary>
    /// 플레이어의 모든 신체(bodyParts)를 참조하여 종족 동화율을 Update한다.
    /// </summary>
    /// <param name="bodyParts">플레이어가 장착한 모든 신체의 배열</param>
    private void UpdateAllBodyAffinity(int[] raceAffinity, BodyPart[] bodyParts)
    {
        // TODO : bodyAffinity를 reset하는 메소드를 Player와 관련없도록 분리하자.
        Player.Inst.ResetBodyAffinity();
        for (int bodyIndex = 0; bodyIndex < bodyParts.Length; bodyIndex++)
        {
            BodyPart playerBodyPart = bodyParts[bodyIndex];
            UpdateBodyAffinity(raceAffinity, playerBodyPart);
        }
    }

    /// <summary>
    /// playerBodyPart의 종족동화율을 raceAffinity에 Update한다.
    /// </summary>
    /// <param name="returnedBodyPart"> 어떤 신체가 플레이어에게서 탈착되었을 경우 사용한다.</param>
    private void UpdateBodyAffinity(int[] raceAffinity, BodyPart playerBodyPart, BodyPart returnedBodyPart = null)
    {
        if (playerBodyPart != null)
        {
            switch (playerBodyPart.bodyPartType)
            {
                case BodyPartType.Head:
                case BodyPartType.Body:
                    raceAffinity[(int)playerBodyPart.race] += 3;
                    //raceAffinity[(int)playerBodyPart.race] += Research.Inst.GetBonusAffinity(playerBodyPart.race, playerBodyPart.bodyPartType);
                    if (returnedBodyPart != null)
                    {
                        raceAffinity[(int)returnedBodyPart.race] -= 3;
                        //raceAffinity[(int)returnedBodyPart.race] -= Research.Inst.GetBonusAffinity(playerBodyPart.race, playerBodyPart.bodyPartType);
                    }
                    break;
                case BodyPartType.LeftArm:
                case BodyPartType.RightArm:
                case BodyPartType.LeftLeg:
                case BodyPartType.RightLeg:
                    raceAffinity[(int)playerBodyPart.race] += 1;
                    //raceAffinity[(int)playerBodyPart.race] += Research.Inst.GetBonusAffinity(playerBodyPart.race, playerBodyPart.bodyPartType);
                    if (returnedBodyPart != null)
                    {
                        raceAffinity[(int)returnedBodyPart.race] -= 1;
                        //raceAffinity[(int)returnedBodyPart.race] -= Research.Inst.GetBonusAffinity(playerBodyPart.race, playerBodyPart.bodyPartType);
                    }
                    break;
                default:
                    Debug.Log("wrong item type");
                    break;
            }
            if (returnedBodyPart != null)
            {
                if (playerBodyPart.race != returnedBodyPart.race)
                {
                    StorageManager.Inst.UpdateToolStat();
                }
            }
        }
        else
        {
            if (returnedBodyPart != null)
            {
                switch(returnedBodyPart.bodyPartType)
                {
                    case BodyPartType.Head:
                    case BodyPartType.Body:
                        raceAffinity[(int)returnedBodyPart.race] -= 3;
                        //raceAffinity[(int)returnedBodyPart.race] -= GameManager.Inst.research.GetBonusAffinity(playerBodyPart.race, playerBodyPart.bodyPartType);
                        break;
                    case BodyPartType.LeftArm:
                    case BodyPartType.RightArm:
                    case BodyPartType.LeftLeg:
                    case BodyPartType.RightLeg:
                        raceAffinity[(int)returnedBodyPart.race] -= 1;
                        //raceAffinity[(int)returnedBodyPart.race] -= GameManager.Inst.research.GetBonusAffinity(playerBodyPart.race, playerBodyPart.bodyPartType);
                        break;
                    default:
                        Debug.Log("wrong item type");
                        break;
                }
                StorageManager.Inst.UpdateToolStat();
            }
        }
    }

    /// <summary>
    /// 플레이어의 모든 신체를 참조하여 플레이어의 bodyPartStatus를 업데이트한다.
    /// </summary>
    private void UpdateAllBodyStat(Status bodyPartStatus, BodyPart[] playerBodyParts)
    {
        bodyPartStatus.ResetStatus();
        for (int bodyIndex = 0; bodyIndex < playerBodyParts.Length; bodyIndex++)
        {
            BodyPart playerBodyPart = playerBodyParts[bodyIndex];
            UpdateBodyStat(bodyPartStatus, playerBodyPart);
        }
    }

    /// <summary>
    /// playerBodyPart의 stat을 bodyPartStatus에 반영한다. 
    /// </summary>
    /// <param name="returnedBodyPart"> 어떤 신체가 플레이어에게서 탈착되었을 경우 사용한다.</param>
    private void UpdateBodyStat(Status bodyPartStatus, BodyPart playerBodyPart, BodyPart returnedBodyPart = null)
    {
        if (playerBodyPart != null)
        {
            if (playerBodyPart.race != Race.Machine)
            {
                bodyPartStatus.atk += playerBodyPart.Atk;
                bodyPartStatus.def += playerBodyPart.Def;
                bodyPartStatus.dex += playerBodyPart.Dex;
                bodyPartStatus.mana += playerBodyPart.Mana;
                if (playerBodyPart.bodyPartType == BodyPartType.Head)
                    bodyPartStatus.nightVision = false;
            }
            else
            {
                Machine machine = (Machine)playerBodyPart;
                bodyPartStatus.atk += machine.Atk;
                bodyPartStatus.def += machine.Def;
                bodyPartStatus.dex += machine.Dex;
                bodyPartStatus.mana += machine.Mana;
                if (playerBodyPart.bodyPartType == BodyPartType.Head)
                    bodyPartStatus.nightVision = machine.nightVision;
            }

            bodyPartStatus.endurance += playerBodyPart.Endurance;
        }
        if(returnedBodyPart != null)
        {
            if (returnedBodyPart.race != Race.Machine)
            {
                bodyPartStatus.atk -= returnedBodyPart.Atk;
                bodyPartStatus.def -= returnedBodyPart.Def;
                bodyPartStatus.dex -= returnedBodyPart.Dex;
                bodyPartStatus.mana -= returnedBodyPart.Mana;
                bodyPartStatus.endurance -= returnedBodyPart.Endurance;
            }
            else
            {
                Machine returnedMachine = (Machine)returnedBodyPart;
                bodyPartStatus.atk -= returnedMachine.Atk;
                bodyPartStatus.def -= returnedMachine.Def;
                bodyPartStatus.dex -= returnedMachine.Dex;
                bodyPartStatus.mana -= returnedMachine.Mana;
                bodyPartStatus.endurance -= returnedMachine.Endurance;
            }
        }
    }
    #endregion

    #region Unity Functions
    protected override void Awake()
    {
        base.Awake();
        InitPlayer();
    }
    #endregion

    public void UpgradeMachinePart(int i)
    {
        _upgradeLevels[i]++;
        UpdateAllPlayerBodyStatus(_raceAffinity, equippedBodyPart.bodyParts, _bodyPartStat);
    }

}

