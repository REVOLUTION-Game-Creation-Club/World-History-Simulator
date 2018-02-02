using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine.Profiling;

public class Culture : ISynchronizable {

	[XmlAttribute]
	public long LanguageId = -1;

	[XmlIgnore]
	public World World;

	[XmlIgnore]
	public Language Language { get; protected set; }

	[XmlArrayItem(Type = typeof(CulturalPreference)),
		XmlArrayItem(Type = typeof(CellCulturalPreference))]
	public List<CulturalPreference> Preferences = new List<CulturalPreference> ();

	[XmlArrayItem(Type = typeof(CulturalActivity)),
		XmlArrayItem(Type = typeof(CellCulturalActivity))]
	public List<CulturalActivity> Activities = new List<CulturalActivity> ();

	[XmlArrayItem(Type = typeof(CulturalSkill)),
		XmlArrayItem(Type = typeof(BiomeSurvivalSkill)),
		XmlArrayItem(Type = typeof(SeafaringSkill))]
	public List<CulturalSkill> Skills = new List<CulturalSkill> ();
	
	[XmlArrayItem(Type = typeof(PolityCulturalKnowledge)),
		XmlArrayItem(Type = typeof(ShipbuildingKnowledge)),
		XmlArrayItem(Type = typeof(AgricultureKnowledge)),
		XmlArrayItem(Type = typeof(SocialOrganizationKnowledge))]
	public List<CulturalKnowledge> Knowledges = new List<CulturalKnowledge> ();
	
	[XmlArrayItem(Type = typeof(PolityCulturalDiscovery)),
		XmlArrayItem(Type = typeof(BoatMakingDiscovery)),
		XmlArrayItem(Type = typeof(SailingDiscovery)),
		XmlArrayItem(Type = typeof(TribalismDiscovery)),
		XmlArrayItem(Type = typeof(PlantCultivationDiscovery))]
	public List<CulturalDiscovery> Discoveries = new List<CulturalDiscovery> ();

	private Dictionary<string, CulturalPreference> _preferences = new Dictionary<string, CulturalPreference> ();
	private Dictionary<string, CulturalActivity> _activities = new Dictionary<string, CulturalActivity> ();
	private Dictionary<string, CulturalSkill> _skills = new Dictionary<string, CulturalSkill> ();
	private Dictionary<string, CulturalKnowledge> _knowledges = new Dictionary<string, CulturalKnowledge> ();
	private Dictionary<string, CulturalDiscovery> _discoveries = new Dictionary<string, CulturalDiscovery> ();
	
	public Culture () {
	}

	public Culture (World world, Language language = null) {

		Language = language;

		World = world;
	}

	public Culture (Culture sourceCulture) : this (sourceCulture.World, sourceCulture.Language) {

		foreach (CulturalPreference p in sourceCulture.Preferences) {
			AddPreference (new CulturalPreference (p));
		}

		foreach (CulturalActivity a in sourceCulture.Activities) {
			AddActivity (new CulturalActivity (a));
		}

		foreach (CulturalSkill s in sourceCulture.Skills) {
			AddSkill (new CulturalSkill (s));
		}

		foreach (CulturalDiscovery d in sourceCulture.Discoveries) {
			AddDiscovery (new CulturalDiscovery (d));
		}

		foreach (CulturalKnowledge k in sourceCulture.Knowledges) {
			AddKnowledge (new CulturalKnowledge (k));
		}
	}

	protected void AddPreference (CulturalPreference preference) {

		if (_preferences.ContainsKey (preference.Id))
			return;

		World.AddExistingCulturalPreferenceInfo (preference);

		Preferences.Add (preference);
		_preferences.Add (preference.Id, preference);
	}

	protected void RemovePreference (CulturalPreference preference) {

		if (!_preferences.ContainsKey (preference.Id))
			return;

		Preferences.Remove (preference);
		_preferences.Remove (preference.Id);
	}

	public void RemovePreference (string preferenceId) {

		CulturalPreference preference = GetPreference (preferenceId);

		if (preference == null)
			return;

		RemovePreference (preference);
	}

	public void ClearPreferences () {

		Preferences.Clear ();
		_preferences.Clear ();
	}

	protected void AddActivity (CulturalActivity activity) {

		if (_activities.ContainsKey (activity.Id))
			return;

		World.AddExistingCulturalActivityInfo (activity);

		Activities.Add (activity);
		_activities.Add (activity.Id, activity);
	}

	protected void RemoveActivity (CulturalActivity activity) {

		if (!_activities.ContainsKey (activity.Id))
			return;

		Activities.Remove (activity);
		_activities.Remove (activity.Id);
	}

	public void RemoveActivity (string activityId) {

		CulturalActivity activity = GetActivity (activityId);

		if (activity == null)
			return;

		RemoveActivity (activity);
	}

	public void ClearActivities () {

		Activities.Clear ();
		_activities.Clear ();
	}
	
	protected void AddSkill (CulturalSkill skill) {

		if (_skills.ContainsKey (skill.Id))
			return;
		
		World.AddExistingCulturalSkillInfo (skill);

		Skills.Add (skill);
		_skills.Add (skill.Id, skill);
	}

	protected void RemoveSkill (CulturalSkill skill) {

		if (!_skills.ContainsKey (skill.Id))
			return;

		Skills.Remove (skill);
		_skills.Remove (skill.Id);
	}

	public void ClearSkills () {

		Skills.Clear ();
		_skills.Clear ();
	}
	
	protected void AddKnowledge (CulturalKnowledge knowledge) {
		
		if (_knowledges.ContainsKey (knowledge.Id))
			return;
		
		World.AddExistingCulturalKnowledgeInfo (knowledge);

		Knowledges.Add (knowledge);
		_knowledges.Add (knowledge.Id, knowledge);
	}

	protected void RemoveKnowledge (CulturalKnowledge knowledge) {

		if (!_knowledges.ContainsKey (knowledge.Id))
			return;

		Knowledges.Remove (knowledge);
		_knowledges.Remove (knowledge.Id);
	}

	public void ClearKnowledges () {

		Knowledges.Clear ();
		_knowledges.Clear ();
	}
	
	protected void AddDiscovery (CulturalDiscovery discovery) {
		
		if (_discoveries.ContainsKey (discovery.Id))
			return;
		
		World.AddExistingCulturalDiscoveryInfo (discovery);

		Discoveries.Add (discovery);
		_discoveries.Add (discovery.Id, discovery);
	}

	protected void RemoveDiscovery (CulturalDiscovery discovery) {

		if (!_discoveries.ContainsKey (discovery.Id))
			return;

		Discoveries.Remove (discovery);
		_discoveries.Remove (discovery.Id);
	}

	public void ClearDiscoveries () {

		Discoveries.Clear ();
		_discoveries.Clear ();
	}

	public CulturalPreference GetPreference (string id) {

		CulturalPreference preference = null;

		if (!_preferences.TryGetValue (id, out preference))
			return null;

		return preference;
	}

	public CulturalActivity GetActivity (string id) {

		CulturalActivity activity = null;

		if (!_activities.TryGetValue (id, out activity))
			return null;

		return activity;
	}
	
	public CulturalSkill GetSkill (string id) {

		CulturalSkill skill = null;

		if (!_skills.TryGetValue (id, out skill))
			return null;
		
		return skill;
	}
	
	public CulturalKnowledge GetKnowledge (string id) {
		
		CulturalKnowledge knowledge = null;
		
		if (!_knowledges.TryGetValue (id, out knowledge))
			return null;
		
		return knowledge;
	}
	
	public CulturalDiscovery GetDiscovery (string id) {
		
		CulturalDiscovery discovery = null;
		
		if (!_discoveries.TryGetValue (id, out discovery))
			return null;
		
		return discovery;
	}

	public void ClearAttributes () {

		ClearPreferences ();
		ClearActivities ();
		ClearSkills ();
		ClearKnowledges ();
		ClearDiscoveries ();
	}

	public virtual void Synchronize () {

		if (Language != null)
			LanguageId = Language.Id;
	}

	public virtual void FinalizeLoad () {

		foreach (CulturalPreference p in Preferences) {
			_preferences.Add (p.Id, p);
		}

		foreach (CulturalActivity a in Activities) {
			_activities.Add (a.Id, a);
		}

		foreach (CulturalSkill s in Skills) {
			_skills.Add (s.Id, s);
		}

		foreach (CulturalKnowledge k in Knowledges) {
			_knowledges.Add (k.Id, k);
		}

		foreach (CulturalDiscovery d in Discoveries) {
			_discoveries.Add (d.Id, d);
		}

		if (LanguageId != -1) {
			Language = World.GetLanguage (LanguageId);
		}
	}
}

public class BufferCulture : Culture {

	public BufferCulture () {
	}

	public BufferCulture (Culture sourceCulture) : base (sourceCulture) {
	}
}
