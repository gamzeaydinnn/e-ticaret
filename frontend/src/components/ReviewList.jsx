//ürüne ait yorumları gösterir
import React from 'react';
import StarRating from './StarRating';

const ReviewList = ({ reviews }) => {
  if (!reviews || reviews.length === 0) {
    return <p>Bu ürün için henüz yorum yapılmamış.</p>;
  }

  return (
    <div className="review-list">
      <h3>Müşteri Yorumları</h3>
      {reviews.map((review) => (
        <div
          key={review.id}
          style={{
            borderBottom: '1px solid #ccc',
            marginBottom: '1rem',
            paddingBottom: '1rem'
          }}
        >
          {/* Backend'den user bilgisi geliyorsa burada gösterebilirsin */}
          <strong>Kullanıcı ID: {review.userId}</strong>
          <StarRating rating={review.rating} />
          <p>{review.comment}</p>
          <small>{new Date(review.createdAt).toLocaleDateString()}</small>
        </div>
      ))}
    </div>
  );
};

export default ReviewList;
