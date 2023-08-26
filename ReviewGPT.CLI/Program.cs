using ReviewGPT.Application;

const string url = "https://github.com/SSWConsulting/SSW.Website/pull/1304";

var reviewService = new GenerateReviewService();

var review = await reviewService.GenerateReview(url);

Console.WriteLine(review);