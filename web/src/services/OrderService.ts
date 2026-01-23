import { IHttpService } from './HttpService';
import { ServiceBase } from './ServiceBase';
import { OrderReview } from '../types/OrderReview';

export class OrderService extends ServiceBase {
  private readonly baseUrl: string = 'api/orders';

  constructor(httpService: IHttpService) {
    super(httpService);
  }

  review = (fileId:string, review: OrderReview): Promise<void> =>
  {
    return this.httpService.patch(
        `${this.baseUrl}/${fileId}/review`,
        review
    );
  };
}
