using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities.mapping
{
    public enum fad3MappingMode
    {
        defaultMode,
        grid25Mode,
        thematicPointMode,
        fishingGroundMappingMode,
        occurenceMappingGear,
        occurenceMappingSpecies,
        occurenceMappingGearAggregated,
        occurenceMappingSpeciesAggregated,
        effortMappingAggregated,
        efforMapping
    }

    public enum ClassificationType
    {
        None,
        NaturalBreaks,
        JenksFisher,
        UniqueValues,
        EqualIntervals,
        EqualCount,
        StandardDeviation,
        EqualSumOfValues
    }

    public enum ExtentCompare
    {
        excoSimilar,
        excoOutside,
        excoInside,
        excoCrossing
    }


}
