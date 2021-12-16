// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.LearnValidation;

public class LearnValidationHelper
{
    private const string DefaultLocale = "en-us";

    private readonly ILearnServiceAccessor _learnServiceAccessor;
    private readonly string _branch;

    public LearnValidationHelper(string branch, ILearnServiceAccessor learnServiceAccessor)
    {
        _learnServiceAccessor = learnServiceAccessor;
        _branch = branch;
    }

    public bool IsModule(string uid)
    {
        return CheckItemExist(CheckItemType.Module, uid);
    }

    public bool IsUnit(string uid)
    {
        return CheckItemExist(CheckItemType.Unit, uid);
    }

    private bool CheckItemExist(CheckItemType type, string uid)
    {
        if (_learnServiceAccessor == null)
        {
            return false;
        }

        var fallbackBranches = _branch switch
        {
            "live" => new string[] { "live" },
            "master" => new string[] { "main", "master" },
            "main" => new string[] { "main", "master" },
            _ => new string[] { _branch, "main", "master" },
        };

        foreach (var branch in fallbackBranches)
        {
            if (_learnServiceAccessor.CheckLearnPathItemExist(branch, DefaultLocale, uid, type).Result)
            {
                return true;
            }
        }
        return false;
    }
}
