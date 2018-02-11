﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine.Profiling;

public delegate float GroupValueCalculationDelegate (CellGroup group);
public delegate float FactionValueCalculationDelegate (Faction faction);

public class PolityInfluence {

	[XmlAttribute]
	public long PolityId;
	[XmlAttribute("Val")]
	public float Value;
	[XmlAttribute("FctDist")]
	public float FactionCoreDistance;
	[XmlAttribute("PolDist")]
	public float PolityCoreDistance;
	[XmlAttribute("Cost")]
	public float AdiministrativeCost;

	[XmlIgnore]
	public float NewValue;
	[XmlIgnore]
	public float NewFactionCoreDistance;
	[XmlIgnore]
	public float NewPolityCoreDistance;

	private bool _isMigratingGroup;

	[XmlIgnore]
	public Polity Polity;

	public PolityInfluence () {

	}

	public PolityInfluence (Polity polity, float value, float polityCoreDistance = -1, float factionCoreDistance = -1) {
	
		PolityId = polity.Id;
		Polity = polity;
		Value = MathUtility.RoundToSixDecimals (value);
		NewValue = Value;

		AdiministrativeCost = 0;

		PolityCoreDistance = polityCoreDistance;
		NewPolityCoreDistance = polityCoreDistance;

		FactionCoreDistance = factionCoreDistance;
		NewFactionCoreDistance = factionCoreDistance;
	}

	public void PostUpdate () {

		Value = NewValue;
		PolityCoreDistance = NewPolityCoreDistance;
		FactionCoreDistance = NewFactionCoreDistance;

		#if DEBUG
		if (FactionCoreDistance == -1) {

			throw new System.Exception ("Core distance is not properly initialized");
		}
		#endif

		#if DEBUG
		if (PolityCoreDistance == -1) {

			throw new System.Exception ("Core distance is not properly initialized");
		}
		#endif
	}
}

public abstract class Polity : ISynchronizable {

	public const float TimeEffectConstant = CellGroup.GenerationSpan * 2500;

	public const float CoreDistanceEffectConstant = 10000;

	public const float MinPolityInfluence = 0.001f;

	[XmlAttribute("Type")]
	public string Type;

	[XmlAttribute]
	public long Id;

	[XmlAttribute("CGrpId")]
	public long CoreGroupId;

	[XmlAttribute("TotalAdmCost")]
	public float TotalAdministrativeCost = 0;

	[XmlAttribute("TotalPop")]
	public float TotalPopulation = 0;

	[XmlAttribute("FctnCount")]
	public int FactionCount { get; private set; }

	[XmlAttribute("StilPres")]
	public bool StillPresent = true;

	[XmlAttribute("DomFactId")]
	public long DominantFactionId;

	[XmlAttribute("IsFoc")]
	public bool IsUnderPlayerFocus = false;

	public List<string> Flags;

	public Name Name;

	public List<long> InfluencedGroupIds;

	public Territory Territory;

	public PolityCulture Culture;

	public List<long> FactionIds;

	public List<long> EventMessageIds;

	[XmlIgnore]
	public World World;

	[XmlIgnore]
	public CellGroup CoreGroup;

	[XmlIgnore]
	public Faction DominantFaction;

	[XmlIgnore]
	public bool WillBeUpdated;

	[XmlIgnore]
	public Dictionary<long, CellGroup> InfluencedGroups = new Dictionary<long, CellGroup> ();

	public Agent CurrentLeader {

		get { 
			return DominantFaction.CurrentLeader;
		}
	}

	protected class WeightedGroup : CollectionUtility.ElementWeightPair<CellGroup> {

		public WeightedGroup (CellGroup group, float weight) : base (group, weight) {

		}
	}

	protected class WeightedFaction : CollectionUtility.ElementWeightPair<Faction> {

		public WeightedFaction (Faction faction, float weight) : base (faction, weight) {

		}
	}

	private Dictionary<long, WeightedGroup> _influencedPopPerGroup = new Dictionary<long, WeightedGroup> ();

	private Dictionary<long, Faction> _factions = new Dictionary<long, Faction> ();

	private HashSet<string> _flags = new HashSet<string> ();

	private bool _willBeRemoved = false;

	#if DEBUG
	private bool _populationCensusUpdated = false;
	#endif

//	private bool _coreGroupIsValid = true;

	private HashSet<long> _eventMessageIds = new HashSet<long> ();

	public Polity () {
	
	}

	protected Polity (string type, CellGroup coreGroup, Polity parentPolity = null) {

		Type = type;

		World = coreGroup.World;

		Territory = new Territory (this);

		CoreGroup = coreGroup;
		CoreGroupId = coreGroup.Id;

		long idOffset = 0;

		if (parentPolity != null) {
		
			idOffset = parentPolity.Id + 1;
		}

		Id = GenerateUniqueIdentifier (World.CurrentDate, 100L, idOffset);

		Culture = new PolityCulture (this);

//		#if DEBUG
//		if (Manager.RegisterDebugEvent != null) {
//			if (CoreGroupId == Manager.TracingData.GroupId) {
//				string groupId = "Id:" + CoreGroupId + "|Long:" + CoreGroup.Longitude + "|Lat:" + CoreGroup.Latitude;
//
//				SaveLoadTest.DebugMessage debugMessage = new SaveLoadTest.DebugMessage(
//					"new Polity - Group:" + groupId + 
//					", Polity.Id: " + Id,
//					"CurrentDate: " + World.CurrentDate  +
//					", CoreGroup:" + groupId + 
//					", Polity.TotalGroupInfluenceValue: " + TotalGroupInfluenceValue + 
//					", coreGroupInfluenceValue: " + coreGroupInfluenceValue + 
//					"");
//
//				Manager.RegisterDebugEvent ("DebugMessage", debugMessage);
//			}
//		}
//		#endif
	}

	public void Initialize () {
	
		Culture.Initialize ();

		InitializeInternal ();
	}

	public abstract void InitializeInternal ();

	public void Destroy () {

		if (IsUnderPlayerFocus) {
			Manager.UnsetFocusOnPolity (this);
		}

		List<Faction> factions = new List<Faction> (_factions.Values);

		foreach (Faction faction in factions) {

			faction.Destroy (true);
		}

		foreach (CellGroup group in InfluencedGroups.Values) {

			group.RemovePolityInfluence (this);

			World.AddGroupToPostUpdate_AfterPolityUpdate (group);
		}
		
		World.RemovePolity (this);

		StillPresent = false;
	}

	public void SetUnderPlayerFocus (bool state, bool setDominantFactionFocused = true) {
	
		IsUnderPlayerFocus = state;
	}

	public void AddEventMessage (WorldEventMessage eventMessage) {

		if (IsUnderPlayerFocus)
			World.AddEventMessageToShow (eventMessage);

		_eventMessageIds.Add (eventMessage.Id);
	}

	public bool HasEventMessage (long id) {
	
		return _eventMessageIds.Contains (id);
	}

	public void SetCoreGroup (CellGroup coreGroup) {
	
		CoreGroup = coreGroup;
		CoreGroupId = coreGroup.Id;

//		_coreGroupIsValid = true;
	}

	public long GenerateUniqueIdentifier (long date, long oom = 1L, long offset = 0L) {

		return CoreGroup.GenerateUniqueIdentifier (date, oom, offset);
	}

	public float GetNextLocalRandomFloat (int iterationOffset) {

		return CoreGroup.GetNextLocalRandomFloat (iterationOffset + (int)Id);
	}

	public int GetNextLocalRandomInt (int iterationOffset, int maxValue) {

		return CoreGroup.GetNextLocalRandomInt (iterationOffset + (int)Id, maxValue);
	}

	public void AddFaction (Faction faction) {

		_factions.Add (faction.Id, faction);

		if (!World.ContainsFaction (faction.Id)) {
			World.AddFaction (faction);
		}

		World.AddFactionToUpdate (faction);

		FactionCount++;
	}

	public void RemoveFaction (Faction faction) {

		_factions.Remove (faction.Id);

		if (_factions.Count <= 0) {
			
			#if DEBUG
			Debug.Log ("Polity will be removed due to losing all factions. faction id: " + faction.Id + ", polity id:" + Id);
			#endif

			PrepareToRemoveFromWorld ();
			return;
		}

		if (DominantFaction == faction) {
			UpdateDominantFaction ();
		}

		World.AddFactionToUpdate (faction);

		FactionCount--;
	}

	public Faction GetFaction (long id) {

		Faction faction;

		_factions.TryGetValue (id, out faction);

		return faction;
	}

	public ICollection<Faction> GetFactions (long id) {

		return _factions.Values;
	}

	public void UpdateDominantFaction () {
	
		Faction mostProminentFaction = null;
		float greatestProminence = float.MinValue;

		foreach (Faction faction in _factions.Values) {
		
			if (faction.Prominence > greatestProminence) {
			
				mostProminentFaction = faction;
				greatestProminence = faction.Prominence;
			}
		}

		if ((mostProminentFaction == null) || (!mostProminentFaction.StillPresent))
			throw new System.Exception ("Faction is null or not present");

		SetDominantFaction (mostProminentFaction);
	}

	public void SetDominantFaction (Faction faction) {

		if (DominantFaction == faction)
			return;

		if (DominantFaction != null) {
		
			DominantFaction.SetDominant (false);
		}

		if ((faction == null) || (!faction.StillPresent))
			throw new System.Exception ("Faction is null or not present");

		if (faction.Polity != this)
			throw new System.Exception ("Faction is not part of polity");
	
		DominantFaction = faction;

		if (faction != null) {
			DominantFactionId = faction.Id;

			faction.SetDominant (true);

			SetCoreGroup (faction.CoreGroup);

			World.AddFactionToUpdate (faction);
		}
	}

	public IEnumerable<Faction> GetFactions () {

		return _factions.Values;
	}

	public IEnumerable<Faction> GetFactions (string type) {

		foreach (Faction faction in _factions.Values) {

			if (faction.Type == type)
				yield return faction;
		}
	}

	public IEnumerable<T> GetFactions<T> () where T : Faction {

		foreach (T faction in _factions.Values) {

				yield return faction;
		}
	}

	public void NormalizeFactionProminences () {
	
		float totalProminence = 0;

		foreach (Faction f in _factions.Values) {
		
			totalProminence += f.Prominence;
		}

		if (totalProminence <= 0) {
			throw new System.Exception ("Total prominence equal or less than zero: " + totalProminence + ", polity id:" + Id);
		}

		foreach (Faction f in _factions.Values) {

			f.Prominence = f.Prominence / totalProminence;
		}
	}

	public void PrepareToRemoveFromWorld () {

		World.AddPolityToRemove (this);

		_willBeRemoved = true;
	}

	public void Update () {

		if (_willBeRemoved) {
			return;
		}

		if (!StillPresent) {
			Debug.LogWarning ("Polity is no longer present. Id: " + Id);

			return;
		}

//		#if DEBUG
//		if (Manager.RegisterDebugEvent != null) {
//			Manager.RegisterDebugEvent ("DebugMessage", 
//				"Update - Polity:" + Id + 
//				", CurrentDate: " + World.CurrentDate + 
//				", InfluencedGroups.Count: " + InfluencedGroups.Count + 
//				", TotalGroupInfluenceValue: " + TotalGroupInfluenceValue + 
//				"");
//		}
//		#endif

		WillBeUpdated = false;

		if (InfluencedGroups.Count <= 0) {

			#if DEBUG
			Debug.Log ("Polity will be removed due to losing all influenced groups. polity id:" + Id);
			#endif

			PrepareToRemoveFromWorld ();
			return;
		}

		Profiler.BeginSample ("Run Population Census");

		RunPopulationCensus ();

		Profiler.EndSample ();

		#if DEBUG
		_populationCensusUpdated = true;
		#endif

		Profiler.BeginSample ("Update Total Administration Cost");

		UpdateTotalAdministrativeCost ();

		Profiler.EndSample ();

		#if DEBUG
		if (IsUnderPlayerFocus) {
			bool debug = true;
		}
		#endif

		Profiler.BeginSample ("Update Culture");
	
		Culture.Update ();

		Profiler.EndSample ();

		Profiler.BeginSample ("Update Internal");

		UpdateInternal ();

		Profiler.EndSample ();

		#if DEBUG
		_populationCensusUpdated = false;
		#endif

		Profiler.BeginSample ("Normalize Faction Prominences");

		NormalizeFactionProminences ();

		Profiler.EndSample ();
	}

	protected abstract void UpdateInternal ();

	public void RunPopulationCensus () {

		TotalPopulation = 0;

		_influencedPopPerGroup.Clear ();
	
		foreach (CellGroup group in InfluencedGroups.Values) {

			float influencedPop = group.Population * group.GetPolityInfluenceValue (this);

			TotalPopulation += influencedPop;

			_influencedPopPerGroup.Add (group.Id, new WeightedGroup (group, influencedPop));
		}
	}

	protected CellGroup GetGroupWithMostInfluencedPop () {

		#if DEBUG
		if (!_populationCensusUpdated)
			Debug.LogWarning ("This function should only be called within polity updates after executing RunPopulationCensus");
		#endif

		CellGroup groupWithMostInfluencedPop = null;
		float maxInfluencedGroupPopulation = 0;
	
		foreach (KeyValuePair<long, WeightedGroup> pair in _influencedPopPerGroup) {
		
			if (maxInfluencedGroupPopulation < pair.Value.Weight) {

				maxInfluencedGroupPopulation = pair.Value.Weight;

				groupWithMostInfluencedPop = pair.Value.Value;
			}
		}

		return groupWithMostInfluencedPop;
	}

	public void AddInfluencedGroup (CellGroup group) {
	
		InfluencedGroups.Add (group.Id, group);
	}

	public void RemoveInfluencedGroup (CellGroup group) {

		InfluencedGroups.Remove (group.Id);
	}

	public virtual void Synchronize () {

		Flags = new List<string> (_flags);

		Culture.Synchronize ();

		Territory.Synchronize ();

		InfluencedGroupIds = new List<long> (InfluencedGroups.Keys);

		FactionIds = new List<long> (_factions.Count);

		foreach (Faction f in _factions.Values) {

			FactionIds.Add (f.Id);
		}

		Name.Synchronize ();

		EventMessageIds = new List<long> (_eventMessageIds);
	}

	public virtual void FinalizeLoad () {

		foreach (long messageId in EventMessageIds) {

			_eventMessageIds.Add (messageId);
		}

		Name.World = World;
		Name.FinalizeLoad ();

		CoreGroup = World.GetGroup (CoreGroupId);

		if (CoreGroup == null) {
			string message = "Missing Group with Id " + CoreGroupId + " in polity with Id " + Id;
			Debug.LogError (message);
		}

		foreach (long id in InfluencedGroupIds) {

			CellGroup group = World.GetGroup (id);

			if (group == null) {
				string message = "Missing Group with Id " + id + " in polity with Id " + Id;
				Debug.LogError (message);
			}

			InfluencedGroups.Add (group.Id, group);
		}

		foreach (long factionId in FactionIds) {

			Faction faction = World.GetFaction (factionId);

			if (faction == null) {
				string message = "Missing Faction with Id " + faction + " in polity with Id " + Id;
				Debug.LogError (message);
			}

			_factions.Add (factionId, faction);
		}

		DominantFaction = GetFaction (DominantFactionId);

		Territory.World = World;
		Territory.Polity = this;
		Territory.FinalizeLoad ();

		Culture.World = World;
		Culture.Polity = this;
		Culture.FinalizeLoad ();

		Flags.ForEach (f => _flags.Add (f));
	}

//	public virtual float CalculateCellMigrationValue (CellGroup sourceGroup, TerrainCell targetCell, float sourceValue)
//	{
//		if (sourceValue <= 0)
//			return 0;
//
//		float sourceGroupTotalPolityInfluenceValue = sourceGroup.TotalPolityInfluenceValue;
//
//		CellGroup targetGroup = targetCell.Group;
//
//		if (targetGroup == null) {
//			return sourceValue / sourceGroupTotalPolityInfluenceValue;
//		}
//
//		float targetGroupTotalPolityInfluenceValue = targetGroup.TotalPolityInfluenceValue;
//
//		if (sourceGroupTotalPolityInfluenceValue <= 0) {
//		
//			throw new System.Exception ("sourceGroup.TotalPolityInfluenceValue equal or less than 0: " + sourceGroupTotalPolityInfluenceValue);
//		}
//
//		float influenceFactor = sourceValue / (targetGroupTotalPolityInfluenceValue + sourceGroupTotalPolityInfluenceValue);
//
//		influenceFactor = MathUtility.RoundToSixDecimals (influenceFactor);
//
////		#if DEBUG
////		if (Manager.RegisterDebugEvent != null) {
////			if (sourceGroup.Id == Manager.TracingData.GroupId) {
////				if (Id == Manager.TracingData.PolityId) {
////					if ((targetCell.Longitude == Manager.TracingData.Longitude) && (targetCell.Latitude == Manager.TracingData.Latitude)) {
////						string sourceGroupId = "Id:" + sourceGroup.Id + "|Long:" + sourceGroup.Longitude + "|Lat:" + sourceGroup.Latitude;
////						string targetLocation = "Long:" + targetCell.Longitude + "|Lat:" + targetCell.Latitude;
////
////						if (targetGroup != null) {
////							targetLocation = "Id:" + targetGroup.Id + "|Long:" + targetGroup.Longitude + "|Lat:" + targetGroup.Latitude;
////						}
////
////						SaveLoadTest.DebugMessage debugMessage = new SaveLoadTest.DebugMessage(
////							"MigrationValue - Group: " + sourceGroupId + 
////							"Polity Id: " + Id,
////							"CurrentDate: " + World.CurrentDate + 
////							", targetLocation: " + targetLocation + 
////							", sourceValue: " + sourceValue.ToString("F7") + 
////							", socialOrgFactor: " + socialOrgFactor.ToString("F7") + 
////							", groupTotalInfluenceValue: " + groupTotalInfluenceValue.ToString("F7") + 
////							", sourceValueFactor: " + sourceValueFactor.ToString("F7") + 
////							", influenceFactor: " + influenceFactor.ToString("F7") + 
////							"");
////
////						Manager.RegisterDebugEvent ("DebugMessage", debugMessage);
////					}
////				}
////			}
////		}
////		#endif
//
//		return Mathf.Clamp01 (influenceFactor);
//	}

	public abstract float CalculateGroupInfluenceExpansionValue (CellGroup sourceGroup, CellGroup targetGroup, float sourceValue);

	public virtual void GroupUpdateEffects (CellGroup group, float influenceValue, float totalPolityInfluenceValue, int timeSpan) {

		if (group.Culture.GetFoundDiscoveryOrToFind (TribalismDiscovery.TribalismDiscoveryId) == null) {

			group.SetPolityInfluence (this, 0);

			return;
		}

		float coreFactionDistance = group.GetFactionCoreDistance (this);

		float coreDistancePlusConstant = coreFactionDistance + CoreDistanceEffectConstant;

		float distanceFactor = 0;

		if (coreDistancePlusConstant > 0)
			distanceFactor = CoreDistanceEffectConstant / coreDistancePlusConstant;

		TerrainCell groupCell = group.Cell;

		float maxTargetValue = 1f;
		float minTargetValue = 0.8f * totalPolityInfluenceValue;

		float randomModifier = groupCell.GetNextLocalRandomFloat (RngOffsets.POLITY_UPDATE_EFFECTS + (int)Id);
		randomModifier *= distanceFactor;
		float targetValue = ((maxTargetValue - minTargetValue) * randomModifier) + minTargetValue;

		float scaledValue = (targetValue - totalPolityInfluenceValue) * influenceValue / totalPolityInfluenceValue;
		targetValue = influenceValue + scaledValue;

		float timeFactor = timeSpan / (float)(timeSpan + TimeEffectConstant);

		influenceValue = (influenceValue * (1 - timeFactor)) + (targetValue * timeFactor);

		influenceValue = Mathf.Clamp01 (influenceValue);

//		#if DEBUG
//		if (Manager.RegisterDebugEvent != null) {
//			if (group.Id == Manager.TracingData.GroupId) {
//				string groupId = "Id:" + group.Id + "|Long:" + group.Longitude + "|Lat:" + group.Latitude;
//
//				SaveLoadTest.DebugMessage debugMessage = new SaveLoadTest.DebugMessage(
//					"UpdateEffects - Group:" + groupId + 
//					", Polity.Id: " + Id,
//					"CurrentDate: " + World.CurrentDate  +
//					", randomFactor: " + randomFactor + 
//					", groupTotalPolityInfluenceValue: " + groupTotalPolityInfluenceValue + 
//					", Polity.TotalGroupInfluenceValue: " + TotalGroupInfluenceValue + 
//					", unmodInflueceValue: " + unmodInflueceValue + 
//					", influenceValue: " + influenceValue + 
//					", group.LastUpdateDate: " + group.LastUpdateDate + 
//					"");
//
//				Manager.RegisterDebugEvent ("DebugMessage", debugMessage);
//			}
//		}
//		#endif

		group.SetPolityInfluence (this, influenceValue);
	}

	public void CalculateAdaptionToCell (TerrainCell cell, out float foragingCapacity, out float survivability) {

		float modifiedForagingCapacity = 0;
		float modifiedSurvivability = 0;

//		Profiler.BeginSample ("Get Polity Skill Values");

		foreach (string biomeName in cell.PresentBiomeNames) {

//			Profiler.BeginSample ("Try Get Polity Biome Survival Skill");

			float biomePresence = cell.GetBiomePresence(biomeName);

			Biome biome = Biome.Biomes [biomeName];

			string skillId = BiomeSurvivalSkill.GenerateId (biome);

			CulturalSkill skill = Culture.GetSkill (skillId);

			if (skill != null) {

//				Profiler.BeginSample ("Evaluate Polity Biome Survival Skill");

				modifiedForagingCapacity += biomePresence * biome.ForagingCapacity * skill.Value;
				modifiedSurvivability += biomePresence * (biome.Survivability + skill.Value * (1 - biome.Survivability));

//				Profiler.EndSample ();

			} else {
				
				modifiedSurvivability += biomePresence * biome.Survivability;
			}

//			Profiler.EndSample ();
		}

//		Profiler.EndSample ();

		float altitudeSurvivabilityFactor = 1 - Mathf.Clamp01 (cell.Altitude / World.MaxPossibleAltitude);

		modifiedSurvivability = (modifiedSurvivability * (1 - cell.FarmlandPercentage)) + cell.FarmlandPercentage;

		foragingCapacity = modifiedForagingCapacity * (1 - cell.FarmlandPercentage);
		survivability = modifiedSurvivability * altitudeSurvivabilityFactor;

		if (survivability > 1) {
			throw new System.Exception ("Modified survivability greater than 1: " + survivability);
		}
	}

	public void UpdateTotalAdministrativeCost () {

		TotalAdministrativeCost = 0;

		foreach (CellGroup group in InfluencedGroups.Values) {

			PolityInfluence pi = group.GetPolityInfluence (this);

			TotalAdministrativeCost += pi.AdiministrativeCost;

			if (TotalAdministrativeCost < 0) {
				TotalAdministrativeCost = float.MaxValue;
				break;
			}
		}
	}

	public CellGroup GetRandomGroup (int rngOffset, GroupValueCalculationDelegate calculateGroupValue, bool nullIfNoValidGroup = false) {

		WeightedGroup[] weightedGroups = new WeightedGroup[InfluencedGroups.Count];

		float totalWeight = 0;

		int index = 0;
		foreach (CellGroup group in InfluencedGroups.Values) {

			float weight = calculateGroupValue (group);

			if (weight < 0)
				throw new System.Exception ("calculateGroupValue method returned weight value less than zero: " + weight);

			if ((weight == 0) && nullIfNoValidGroup)
				continue;

			totalWeight += weight;

			weightedGroups [index] = new WeightedGroup (group, weight);
			index++;
		}

		if (totalWeight < 0) {
		
			throw new System.Exception ("Total weight can't be less than zero: " + totalWeight);
		}

		if ((totalWeight == 0) && nullIfNoValidGroup) {
		
			return null;
		}

		return CollectionUtility.WeightedSelection (weightedGroups, totalWeight, () => GetNextLocalRandomFloat (rngOffset));
	}

	public Faction GetRandomFaction (int rngOffset, FactionValueCalculationDelegate calculateFactionValue, bool nullIfNoValidFaction = false) {

		WeightedFaction[] weightedFactions = new WeightedFaction[_factions.Count];

		float totalWeight = 0;

		int index = 0;
		foreach (Faction faction in _factions.Values) {

			float weight = calculateFactionValue (faction);

			if (weight < 0)
				throw new System.Exception ("calculateFactionValue method returned weight value less than zero: " + weight);

			totalWeight += weight;

			weightedFactions [index] = new WeightedFaction (faction, weight);
			index++;
		}

		if (totalWeight < 0) {

			throw new System.Exception ("Total weight can't be less than zero: " + totalWeight);
		}

		if ((totalWeight == 0) && nullIfNoValidFaction) {

			return null;
		}

		return CollectionUtility.WeightedSelection (weightedFactions, totalWeight, () => GetNextLocalRandomFloat (rngOffset));
	}

	protected abstract void GenerateName ();

	public void SetFlag (string flag) {

		if (_flags.Contains (flag))
			return;

		_flags.Add (flag);
	}

	public bool IsFlagSet (string flag) {

		return _flags.Contains (flag);
	}

	public void UnsetFlag (string flag) {

		if (!_flags.Contains (flag))
			return;

		_flags.Remove (flag);
	}
}

public abstract class PolityEventMessage : WorldEventMessage {

	[XmlAttribute]
	public long PolityId;

	[XmlIgnore]
	public Polity Polity {
		get { return World.GetPolity (PolityId); }
	}

	public PolityEventMessage () {

	}

	public PolityEventMessage (Polity polity, long id, long date) : base (polity.World, id, date) {

		PolityId = polity.Id;
	}
}

public abstract class PolityEvent : WorldEvent {

	[XmlAttribute]
	public long PolityId;

	[XmlAttribute]
	public long EventTypeId;

	[XmlIgnore]
	public Polity Polity;

	public PolityEvent () {

	}

	public PolityEvent (Polity polity, int triggerDate, long eventTypeId) : base (polity.World, triggerDate, GenerateUniqueIdentifier (polity, triggerDate, eventTypeId)) {

		Polity = polity;
		PolityId = Polity.Id;

		EventTypeId = eventTypeId;

//		#if DEBUG
//		if (Manager.RegisterDebugEvent != null) {
//			string polityId = "Id: " + polity.Id;
//
//			SaveLoadTest.DebugMessage debugMessage = new SaveLoadTest.DebugMessage("PolityEvent - Polity: " + polityId, "TriggerDate: " + TriggerDate);
//
//			Manager.RegisterDebugEvent ("DebugMessage", debugMessage);
//		}
//		#endif
	}

	public static long GenerateUniqueIdentifier (Polity polity, int triggerDate, long eventTypeId) {

		return ((long)triggerDate * 1000000000L) + ((polity.Id % 1000000L) * 1000L) + eventTypeId;
	}

	public override bool IsStillValid () {
	
		if (!base.IsStillValid ())
			return false;

		if (Polity == null)
			return false;

		return Polity.StillPresent;
	}

	public override void FinalizeLoad () {

		base.FinalizeLoad ();

		Polity = World.GetPolity (PolityId);

		if (Polity == null) {

			Debug.LogError ("PolityEvent: Polity with Id:" + PolityId + " not found");
		}
	}

	public virtual void Reset (int newTriggerDate) {

		Reset (newTriggerDate, GenerateUniqueIdentifier (Polity, newTriggerDate, EventTypeId));
	}
}

