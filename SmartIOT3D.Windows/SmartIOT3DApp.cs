using SmartIOT.Stride.Library;
using Stride.Engine;

using var game = new Game();
game.Services.AddService(new RoutingTable());

game.Run();
