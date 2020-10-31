﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class SplitPolityAttribute : EffectEntityAttribute
{
    private PolityEntity _polityEntity;

    private readonly IValueExpression<string> _newPolityTypeExp;
    private readonly IValueExpression<IEntity> _splittingFactionExp;

    public SplitPolityAttribute(PolityEntity polityEntity, IExpression[] arguments)
        : base(PolityEntity.SplitAttributeId, polityEntity, arguments, 2)
    {
        _polityEntity = polityEntity;

        _newPolityTypeExp = ValueExpressionBuilder.ValidateValueExpression<string>(arguments[0]);
        _splittingFactionExp = ValueExpressionBuilder.ValidateValueExpression<IEntity>(arguments[1]);
    }

    public override void Apply()
    {
        FactionEntity factionEntity = _splittingFactionExp.Value as FactionEntity;

        if (factionEntity == null)
        {
            throw new System.ArgumentException(
                "split: invalid splitting faction: " +
                "\n - expression: " + ToString() +
                "\n - new polity type: " + _newPolityTypeExp.ToPartiallyEvaluatedString() +
                "\n - splitting faction: " + _splittingFactionExp.ToPartiallyEvaluatedString());
        }

        string typeValue = _newPolityTypeExp.Value;

        if (!Polity.ValidateType(typeValue))
        {
            throw new System.ArgumentException(
                "split: invalid polity type: " +
                "\n - expression: " + ToString() +
                "\n - new polity type: " + _newPolityTypeExp.ToPartiallyEvaluatedString() +
                "\n - splitting faction: " + _splittingFactionExp.ToPartiallyEvaluatedString());
        }

        _polityEntity.Polity.Split(typeValue, factionEntity.Faction);
    }
}
