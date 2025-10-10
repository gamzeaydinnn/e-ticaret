//yıldız puanlama 
import React from 'react';

const StarRating = ({ rating }) => {
  const totalStars = 5;

  return (
    <div>
      {[...Array(totalStars)].map((_, index) => (
        <span
          key={index}
          style={{
            color: index < rating ? 'gold' : 'gray',
            fontSize: '1.2rem',
            marginRight: '2px'
          }}
        >
          &#9733; {/* Yıldız karakteri */}
        </span>
      ))}
    </div>
  );
};

export default StarRating;
 