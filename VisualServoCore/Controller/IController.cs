namespace VisualServoCore.Controller
{
    public interface IController<TInput, TOutput>
    {

        public LogObject<TOutput> Run(TInput input);

    }
}
