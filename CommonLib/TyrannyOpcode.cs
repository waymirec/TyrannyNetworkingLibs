
namespace Tyranny.Networking
{
    public enum TyrannyOpcode
    {
        NoOp=0,
        AuthIdent=1,
        AuthChallenge=2,
        AuthProof=3,
        AuthProofAck=4,
        AuthProofAckAck=5,
        AuthComplete=6,

        GameIdent=10,
        GameReady=11,
        Ping=21,
        Pong=22,

        Hello=50,

        EnterWorld=100,
        SpawnWorldEntity=101,
        DestroyWorldEntity=102,
        MoveWorldEntity=103
    }
}
