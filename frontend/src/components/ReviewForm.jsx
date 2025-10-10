//Kullanıcının yeni yorum eklemesini sağlar.

import React, { useState } from 'react';
import reviewService from '../services/reviewService';

const ReviewForm = ({ productId, onReviewSubmitted }) => {
  const [rating, setRating] = useState(0);
  const [comment, setComment] = useState('');
  const [error, setError] = useState('');

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (rating === 0 || comment.trim() === '') {
      setError('Lütfen puan verin ve bir yorum yazın.');
      return;
    }

    try {
      const reviewData = { rating, comment };
      await reviewService.createReview(productId, reviewData);
      onReviewSubmitted(); // Listeyi yeniler
      setRating(0);
      setComment('');
      setError('');
    } catch (err) {
      setError('Yorum gönderilirken bir hata oluştu. Lütfen daha sonra tekrar deneyin.');
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <h4>Yorum Yap</h4>
      {error && <p style={{ color: 'red' }}>{error}</p>}

      <div>
        <label>Puanınız:</label>
        {/* Gelişmiş yıldız seçme component'i buraya entegre edilebilir */}
        <input
          type="number"
          min="1"
          max="5"
          value={rating}
          onChange={(e) => setRating(parseInt(e.target.value, 10))}
          required
        />
      </div>

      <div>
        <label>Yorumunuz:</label>
        <textarea
          value={comment}
          onChange={(e) => setComment(e.target.value)}
          required
        />
      </div>

      <button type="submit">Gönder</button>
    </form>
  );
};

export default ReviewForm;
