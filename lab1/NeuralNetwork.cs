public class NeuralNetwork
{
    private NeuronLayer Hidden1;
    private NeuronLayer Hidden2;
    private NeuronLayer Output;

    private double LearningRate = 0.1;

    public NeuralNetwork()
    {
        Hidden1 = new NeuronLayer(64, 32);
        Hidden2 = new NeuronLayer(32, 16);
        Output = new NeuronLayer(16, 10);
    }

    public double[] FeedForward(double[] inputs)
    {
        var h1 = Hidden1.FeedForward(inputs);
        var h2 = Hidden2.FeedForward(h1);
        var output = Output.FeedForward(h2, useSoftmax: true);
        return output;
    }

    public void BackPropagate(double[] inputs, double[] expected)
    {
        var output = FeedForward(inputs);

        // Выходной слой: производная softmax+crossentropy = output - expected
        for (int i = 0; i < Output.NeuronCount; i++)
            Output.Deltas[i] = output[i] - expected[i];

        // Hidden2 слой
        for (int i = 0; i < Hidden2.NeuronCount; i++)
        {
            double sum = 0;
            for (int j = 0; j < Output.NeuronCount; j++)
                sum += Output.Weights[j, i] * Output.Deltas[j];

            Hidden2.Deltas[i] = sum * NeuronLayer.SigmoidDerivative(Hidden2.Outputs[i]);
        }

        // Hidden1 слой
        for (int i = 0; i < Hidden1.NeuronCount; i++)
        {
            double sum = 0;
            for (int j = 0; j < Hidden2.NeuronCount; j++)
                sum += Hidden2.Weights[j, i] * Hidden2.Deltas[j];

            Hidden1.Deltas[i] = sum * NeuronLayer.SigmoidDerivative(Hidden1.Outputs[i]);
        }

        // Обновление весов (Output)
        for (int i = 0; i < Output.NeuronCount; i++)
        {
            for (int j = 0; j < Output.InputCount; j++)
                Output.Weights[i, j] -= LearningRate * Output.Deltas[i] * Hidden2.Outputs[j];
            Output.Biases[i] -= LearningRate * Output.Deltas[i];
        }

        // Hidden2
        for (int i = 0; i < Hidden2.NeuronCount; i++)
        {
            for (int j = 0; j < Hidden2.InputCount; j++)
                Hidden2.Weights[i, j] -= LearningRate * Hidden2.Deltas[i] * Hidden1.Outputs[j];
            Hidden2.Biases[i] -= LearningRate * Hidden2.Deltas[i];
        }

        // Hidden1
        for (int i = 0; i < Hidden1.NeuronCount; i++)
        {
            for (int j = 0; j < Hidden1.InputCount; j++)
                Hidden1.Weights[i, j] -= LearningRate * Hidden1.Deltas[i] * inputs[j];
            Hidden1.Biases[i] -= LearningRate * Hidden1.Deltas[i];
        }
    }


    public void Train(List<(double[] Input, double[] Label)> dataset, int epochs)
    {
        for (int e = 0; e < epochs; e++)
        {
            foreach (var (input, label) in dataset)
                BackPropagate(input, label);
        }
    }
}
