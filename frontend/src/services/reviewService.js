import api from './api'; // Projendeki axios instance'ını import et

const reviewService = {
  // Bir ürüne ait yorumları getirir
  getReviewsByProductId: (productId) => {
    return api.get(`/products/${productId}/reviews`);
  },

  // Yeni bir yorum ekler
  createReview: (productId, reviewData) => {
    // reviewData: { rating: 5, comment: "Harika ürün!" }
    return api.post(`/products/${productId}/reviews`, reviewData);
  },
};

export default reviewService;
