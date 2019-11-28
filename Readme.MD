InfluenceMapper had a function where it multiplied every instance of the influencemaps on the CPU, so we moved the computetation to the GPU and multithreaded the calculations. GPU are faster at multiplying float then the CPU. In the CalculateMap() function you can see both of the main calls.

Johan Andersson & Simon Larsson