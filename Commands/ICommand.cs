using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManagerCLI.Commands;

public interface ICommand
{
    Task<string> ExecuteAsync(string[] parameters);

}
