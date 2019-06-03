﻿using UnityEngine;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Linq;

public class RegionInfo : ISynchronizable, IKeyedValue<long>
{
    [XmlAttribute]
    public long Id;

    [XmlAttribute("LId")]
    public long LanguageId;

    [XmlAttribute("ED")]
    public long EstablishmentDate;

    public Region Region;

    public WorldPosition OriginCellPosition;

#if DEBUG
    [XmlIgnore]
    public List<string> ElementIds = new List<string>();
    [XmlIgnore]
    public List<string> AttributeNames = new List<string>();
#endif

    [XmlIgnore]
    public Dictionary<string, RegionAttribute.Instance> Attributes = new Dictionary<string, RegionAttribute.Instance>();
    [XmlIgnore]
    public List<RegionAttribute.Instance> AttributeList = new List<RegionAttribute.Instance>();

    [XmlIgnore]
    public List<Element.Instance> Elements = new List<Element.Instance>();

    [XmlIgnore]
    public World World;

    [XmlIgnore]
    public Language Language;

    [XmlIgnore]
    public TerrainCell OriginCell;
    
    public Name Name
    {
        get
        {
            if (_name == null)
            {
                GenerateName();
            }

            return _name;
        }
    }

    private Name _name = null;

    private int _rngOffset;

    public RegionInfo()
    {

    }

    public RegionInfo(Region region, TerrainCell originCell, Language language)
    {
        World = originCell.World;

        EstablishmentDate = World.CurrentDate;

        Id = originCell.GenerateUniqueIdentifier(EstablishmentDate);
        Region = region;

        OriginCell = originCell;
        OriginCellPosition = originCell.Position;

        Language = language;
        LanguageId = language.Id;
    }

    public void AddAttribute(RegionAttribute.Instance attribute)
    {
#if DEBUG
        AttributeNames.Add(attribute.Name);
#endif

        Attributes.Add(attribute.Id, attribute);
        AttributeList.Add(attribute);
    }

    public void AddElement(Element.Instance element)
    {
#if DEBUG
        ElementIds.Add(element.Id);
#endif

        Elements.Add(element);
    }

    public virtual void Synchronize()
    {
        if (Region != null)
            Region.Synchronize();
    }

    public virtual void FinalizeLoad()
    {
        OriginCell = World.GetCell(OriginCellPosition);

        Language = World.GetLanguage(LanguageId);

        //foreach (string attrName in AttributeNames)
        //{
        //    Attributes.Add(RegionAttribute.Attributes[attrName]);
        //}

        //foreach (string elemName in ElementIds)
        //{
        //    Elements.Add(Element.Elements[elemName]);
        //}

        if (Region != null)
        {
            Region.Info = this;
            Region.FinalizeLoad();
        }
    }

    public string GetRandomAttributeVariation(GetRandomIntDelegate getRandomInt)
    {
        if (Attributes.Count <= 0)
        {
            return string.Empty;
        }

        int index = getRandomInt(Attributes.Count);

        return AttributeList[index].GetRandomVariation(getRandomInt);
    }

    protected void AddElements(IEnumerable<Element.Instance> elem)
    {
        Elements.AddRange(elem);
    }

    public string GetRandomUnstranslatedAreaName(GetRandomIntDelegate getRandomInt, bool isNounAdjunct)
    {
        string untranslatedName;

        Element.Instance elementInstance = Elements.RandomSelect(getRandomInt, isNounAdjunct ? 5 : 20);
        Element element = null;

        List<RegionAttribute.Instance> remainingAttributes = new List<RegionAttribute.Instance>(AttributeList);

        RegionAttribute.Instance attribute = remainingAttributes.RandomSelectAndRemove(getRandomInt);

        List<string> possibleAdjectives = attribute.Adjectives;

        bool addAttributeNoun = true;

        int wordCount = 0;

        if (elementInstance != null)
        {
            possibleAdjectives = elementInstance.Adjectives;

            wordCount++;

            if (isNounAdjunct && (getRandomInt(10) > 4))
            {

                addAttributeNoun = false;
            }

            element = elementInstance.Element;
        }

        string attributeNoun = string.Empty;

        if (addAttributeNoun)
        {
            attributeNoun = attribute.GetRandomVariation(getRandomInt, element);

            wordCount++;
        }

        int nullAdjectives = 4 * wordCount * (isNounAdjunct ? 4 : 1);

        string adjective = possibleAdjectives.RandomSelect(getRandomInt, nullAdjectives);
        if (!string.IsNullOrEmpty(adjective))
            adjective = "[adj]" + adjective + " ";

        string elementNoun = string.Empty;
        if (elementInstance != null)
            elementNoun = "[nad]" + elementInstance.SingularName + ((addAttributeNoun) ? " " : string.Empty);

        untranslatedName = adjective + elementNoun;

        if (isNounAdjunct)
        {
            untranslatedName += (addAttributeNoun) ? ("[nad]" + attributeNoun) : string.Empty;
        }
        else
        {
            untranslatedName += attributeNoun;
        }

//#if DEBUG
//        if ((Manager.RegisterDebugEvent != null) && (Manager.TracingData.Priority <= 0))
//        {
//            //if (Manager.TracingData.RegionId == Id)
//            //{
//            SaveLoadTest.DebugMessage debugMessage = new SaveLoadTest.DebugMessage(
//                "RegionInfo.GetRandomUnstranslatedAreaName - Region.Id:" + Id,
//                "CurrentDate: " + World.CurrentDate +
//                ", EstablishmentDate: " + EstablishmentDate +
//                ", attribute.Name: " + attribute.Name +
//                ", Attributes.Count: " + Attributes.Count +
//                ", AttributeNames: [" + string.Join(",", AttributeNames.ToArray()) + "]" +
//                ", nullAdjectives: " + nullAdjectives +
//                ", possibleAdjectives: [" + string.Join(",", possibleAdjectives) + "]" +
//                ", untranslatedName: " + untranslatedName +
//                "");

//            Manager.RegisterDebugEvent("DebugMessage", debugMessage);
//            //}
//        }
//#endif

        return untranslatedName;
    }

    private int GetRandomInt(int maxValue)
    {
        return OriginCell.GetLocalRandomInt(EstablishmentDate, _rngOffset++, maxValue);
    }

    private float GetRandomFloat()
    {
        return OriginCell.GetLocalRandomFloat(EstablishmentDate, _rngOffset++);
    }

    private void GenerateName()
    {
        _rngOffset = RngOffsets.REGION_GENERATE_NAME + unchecked((int)Language.Id);

        string untranslatedName;

        int wordCount = 1;

        List<RegionAttribute.Instance> remainingAttributes = new List<RegionAttribute.Instance>(AttributeList);

        RegionAttribute.Instance primaryAttribute = remainingAttributes.RandomSelectAndRemove(GetRandomInt);

        List<Element.Instance> remainingElements = new List<Element.Instance>(Elements);

        Element.Instance firstElementInstance = remainingElements.RandomSelect(GetRandomInt, 5, true);
        Element firstElement = null;

        IEnumerable<string> possibleAdjectives = primaryAttribute.Adjectives;

        if (firstElementInstance != null)
        {
            possibleAdjectives = firstElementInstance.Adjectives;

            wordCount++;

            firstElement = firstElementInstance.Element;
        }

        string primaryAttributeNoun = primaryAttribute.GetRandomVariation(GetRandomInt, firstElement);

        string secondaryAttributeNoun = string.Empty;

        int elementFactor = (firstElementInstance != null) ? 8 : 4;

        float secondaryAttributeChance = 4f / (elementFactor + possibleAdjectives.Count());

        if ((remainingAttributes.Count > 0) && (GetRandomFloat() < secondaryAttributeChance))
        {
            RegionAttribute.Instance secondaryAttribute = remainingAttributes.RandomSelectAndRemove(GetRandomInt);

            if (firstElementInstance == null)
            {
                possibleAdjectives = possibleAdjectives.Union(secondaryAttribute.Adjectives);
            }

            secondaryAttributeNoun = "[nad]" + secondaryAttribute.GetRandomVariation(GetRandomInt, firstElement) + " ";

            wordCount++;
        }

        string adjective = possibleAdjectives.RandomSelect(GetRandomInt, (int)Mathf.Pow(2, wordCount));

        if (!string.IsNullOrEmpty(adjective))
            adjective = "[adj]" + adjective + " ";

        string elementNoun = string.Empty;
        if (firstElementInstance != null)
        {
            elementNoun = "[nad]" + firstElementInstance.SingularName + " ";
        }

        untranslatedName = "[Proper][NP](" + adjective + elementNoun + secondaryAttributeNoun + primaryAttributeNoun + ")";

        _name = new Name(untranslatedName, Language, World);
    }

    public long GetKey()
    {
        return Id;
    }
}
