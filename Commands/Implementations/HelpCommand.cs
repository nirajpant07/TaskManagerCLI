namespace TaskManagerCLI.Commands.Implementations
{
    public class HelpCommand : ICommand
    {
        private readonly ICommandFactory _commandFactory;

        public HelpCommand(ICommandFactory commandFactory)
        {
            _commandFactory = commandFactory;
        }

        public async Task<string> ExecuteAsync(string[] parameters)
        {
            return _commandFactory.GetHelpText();
        }
    }
}