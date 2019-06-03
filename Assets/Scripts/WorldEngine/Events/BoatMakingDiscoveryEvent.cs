﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine.Profiling;

public class BoatMakingDiscoveryEvent : DiscoveryEvent
{
    public const long DateSpanFactorConstant = CellGroup.GenerationSpan * 10000;

    public const string EventSetFlag = "BoatMakingDiscoveryEvent_Set";

    public BoatMakingDiscoveryEvent()
    {

    }

    public BoatMakingDiscoveryEvent(CellGroup group, long triggerDate) : base(group, triggerDate, BoatMakingDiscoveryEventId)
    {
        Group.SetFlag(EventSetFlag);
    }

    public static long CalculateTriggerDate(CellGroup group)
    {
        float oceanPresence = ShipbuildingKnowledge.CalculateNeighborhoodSeaPresenceIn(group);

        float randomFactor = group.Cell.GetNextLocalRandomFloat(RngOffsets.BOAT_MAKING_DISCOVERY_EVENT_CALCULATE_TRIGGER_DATE);
        randomFactor = randomFactor * randomFactor;

        float dateSpan = (1 - randomFactor) * DateSpanFactorConstant;

        if (oceanPresence > 0)
        {
            dateSpan /= oceanPresence;
        }
        else
        {
            throw new System.Exception("Can't calculate valid trigger date");
        }

        long targetDate = (long)(group.World.CurrentDate + dateSpan) + 1;

        return targetDate;
    }

    public static bool CanSpawnIn(CellGroup group)
    {
        if (group.IsFlagSet(EventSetFlag))
            return false;

        if (group.Culture.HasKnowledge(ShipbuildingKnowledge.KnowledgeId))
            return false;

        float oceanPresence = ShipbuildingKnowledge.CalculateNeighborhoodSeaPresenceIn(group);

        return (oceanPresence > 0);
    }

    public override bool CanTrigger()
    {
        if (!base.CanTrigger())
            return false;

        if (Group.Culture.HasKnowledge(ShipbuildingKnowledge.KnowledgeId))
            return false;

        return true;
    }

    public override void Trigger()
    {
        Group.Culture.TryAddDiscoveryToFind(BoatMakingDiscovery.DiscoveryId);
        Group.Culture.TryAddKnowledgeToLearn(ShipbuildingKnowledge.KnowledgeId, Group, ShipbuildingKnowledge.InitialValue);
        World.AddGroupToUpdate(Group);

        TryGenerateEventMessage(BoatMakingDiscoveryEventId, BoatMakingDiscovery.DiscoveryId);
    }

    protected override void DestroyInternal()
    {
        if (Group != null)
        {
            Group.UnsetFlag(EventSetFlag);
        }

        base.DestroyInternal();
    }
}
