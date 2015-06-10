﻿using UnityEngine;
using System.Collections;
using System.Xml;
using System.Xml.Serialization;

public class Biome {

	public static Biome IceCap = new Biome(
		"Ice Cap", 
		0,
		World.MinPossibleAltitude, 
		World.MaxPossibleAltitude, 
		World.MinPossibleRainfall,
		World.MaxPossibleRainfall,
		World.MinPossibleTemperature,
		-15f);
	
	public static Biome Ocean = new Biome(
		"Ocean",
		1,
		World.MinPossibleAltitude, 
		0, 
		World.MinPossibleRainfall,
		World.MaxPossibleRainfall,
		-15f,
		World.MaxPossibleTemperature);
	
	public static Biome Grassland = new Biome(
		"Grassland",
		2,
		0, 
		World.MaxPossibleAltitude, 
		25f,
		1475f,
		-5f,
		World.MaxPossibleTemperature);
	
	public static Biome TemperateForest = new Biome(
		"Temperate Forest", 
		3,
		0, 
		World.MaxPossibleAltitude, 
		975f,
		World.MaxPossibleRainfall,
		-5f,
		World.MaxPossibleTemperature);
	
	public static Biome Taiga = new Biome(
		"Taiga", 
		4,
		0, 
		World.MaxPossibleAltitude, 
		275f,
		World.MaxPossibleRainfall,
		-15f,
		-0f);
	
	public static Biome Tundra = new Biome(
		"Tundra", 
		5,
		0, 
		World.MaxPossibleAltitude, 
		World.MinPossibleRainfall,
		725f,
		-20f,
		-0f);
	
	public static Biome Desert = new Biome(
		"Desert", 
		6,
		0, 
		World.MaxPossibleAltitude, 
		World.MinPossibleRainfall,
		125f,
		-5f,
		World.MaxPossibleTemperature);

	public static Biome[] Biomes = new Biome[] {

		IceCap,
		Ocean,
		Grassland,
		TemperateForest,
		Taiga,
		Tundra,
		Desert
	};
	
	[XmlAttribute]
	public string Name;

	[XmlAttribute]
	public float MinAltitude;
	[XmlAttribute]
	public float MaxAltitude;

	[XmlAttribute]
	public float MinRainfall;
	[XmlAttribute]
	public float MaxRainfall;

	[XmlAttribute]
	public float MinTemperature;
	[XmlAttribute]
	public float MaxTemperature;

	public int ColorId;

	public Biome () {
	}

	public Biome (string name, int colorId, float minAltitude, float maxAltitude, float minRainfall, float maxRainfall, float minTemperature, float maxTemperature) {

		Name = name;

		ColorId = colorId;

		MinAltitude = minAltitude;
		MaxAltitude = maxAltitude;
		MinRainfall = minRainfall;
		MaxRainfall = maxRainfall;
		MinTemperature = minTemperature;
		MaxTemperature = maxTemperature;
	}
}
