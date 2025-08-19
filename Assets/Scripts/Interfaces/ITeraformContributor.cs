using System;
using System.Collections.Generic;

public interface ITerraformContributor {
    // Return current factors
    List<TerraformFactor> GetTerraformFactors();
    List<TerraformFactor> GetTerraformFactorsMax();

    // Fired when values change so the manager can re-aggregate
    event Action OnFactorsChanged;
}