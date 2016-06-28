﻿using UnityEngine;
using System.Collections;

public class TestRunnerScript : MonoBehaviour {

	public AutomatedTest[] tests = new AutomatedTest[] {
		new SaveLoadTest (80, 1, 1, true),
		new SaveLoadTest (100000, 200000, 200000, false)
	};

	private int _prevTestIndex = -1;
	private int _testIndex = 0;

	private int _successes = 0;

	// Use this for initialization
	void Start () {

		Manager.RecordingEnabled = true;

		Debug.Log ("Running Tests...\n");
	}
	
	// Update is called once per frame
	void Update () {

		Manager.ExecuteTasks (100);

		if (_testIndex == tests.Length) {

			Debug.Log ("\nFinished Tests!");
			Debug.Log (_successes + " of " + tests.Length + " Succeded");
			Debug.Break ();

		} else {

			AutomatedTest test = tests [_testIndex];

			if (_prevTestIndex != _testIndex) {
				
				Debug.Log ("Executing test: " + _testIndex + " - " + test.Name);

				_prevTestIndex = _testIndex;
			}

			test.Run ();

			_successes += (test.State == TestState.Succeded) ? 1 : 0;

			if ((test.State == TestState.Succeded) || (test.State == TestState.Failed))
				_testIndex++;
		}
	}
}
