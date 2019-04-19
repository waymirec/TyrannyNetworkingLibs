
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

        GameIdent=20,
        Ping=21,
        Pong=22,

        Hello=50,

        EnterWorld=100,
        Spawn=101,
        Despawn=102,
        Move=103
    }
}
