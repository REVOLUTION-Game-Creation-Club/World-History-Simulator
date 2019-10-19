﻿using ProtoBuf;

[ProtoContract]
public class AvoidMergeTribesAttemptEventMessage : PolityEventMessage {

	[ProtoMember(1)]
	public long AgentId;

	[ProtoMember(2)]
	public long SourceTribeId;

	[ProtoMember(3)]
	public long TargetTribeId;

	public AvoidMergeTribesAttemptEventMessage () {

	}

	public AvoidMergeTribesAttemptEventMessage (Tribe sourceTribe, Tribe targetTribe, Agent agent, long date) : base (sourceTribe, WorldEvent.AvoidMergeTribesAttemptDecisionEventId, date) {

		sourceTribe.World.AddMemorableAgent (agent);

		AgentId = agent.Id;
		SourceTribeId = sourceTribe.Id;
		TargetTribeId = targetTribe.Id;
	}

    protected override string GenerateMessage()
    {
        Agent leader = World.GetMemorableAgent(AgentId);
        PolityInfo sourceTribeInfo = World.GetPolityInfo(SourceTribeId);
        PolityInfo targetTribeInfo = World.GetPolityInfo(TargetTribeId);

        return leader.Name.BoldText + ", leader of " + sourceTribeInfo.GetNameAndTypeStringBold() + ", has decided not to propose " +
            targetTribeInfo.GetNameAndTypeStringBold() + " merge with " + leader.PossessiveNoun + " tribe";
    }
}
