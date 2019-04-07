using System;
using System.Collections.Generic;
using System.Text;

namespace Tyranny.Networking
{
    public enum TyrannyOpcode
    {
        AuthIdent=1,
        AuthChallenge=2,
        AuthProof=3,
        AuthProofAck=4,
        AuthProofAckAck=5,
        AuthComplete=6,

        GameIdent=20,
        Ping=21,
        Pong=22
    }
}
