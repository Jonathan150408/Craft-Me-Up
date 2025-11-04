using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShootMeUp.Model
{
    /// <summary>
    /// This class is used as a base for every handler to use if they need to check if two CFrames are overlapping
    /// </summary>
    public class Handler
    {
        /// <summary>
        /// Create a new Handler
        /// </summary>
        public Handler() { }

        /// <summary>
        /// A helper method to check if two CFrames overlap
        /// </summary>
        /// <param name="cfrA">The first CFrame</param>
        /// <param name="cfrB">The second CFrame</param>
        /// <returns></returns>
        protected bool IsOverlapping(CFrame cfrA, CFrame cfrB)
        {
            bool overlapX = cfrA.X < cfrB.X + cfrB.Size && cfrA.X + cfrA.Size > cfrB.X;
            bool overlapY = cfrA.Y < cfrB.Y + cfrB.Size && cfrA.Y + cfrA.Size > cfrB.Y;
            return overlapX && overlapY;
        }
    }
}
