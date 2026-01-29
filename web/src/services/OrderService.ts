import { Order, OrderReview } from '@/types';
import { IHttpService } from './HttpService';
import { ServiceBase } from './ServiceBase';

export class OrderService extends ServiceBase {
  private readonly baseUrl: string = 'api/orders';

  constructor(httpService: IHttpService) {
    super(httpService);
  }

  review = (orderId: string, review: OrderReview): Promise<void> => {
    return this.httpService.patch(`${this.baseUrl}/${orderId}/review`, review);
  };

  getOrders(judgeId: number | null = null): Promise<Order[]> {
    return this.httpService.get<Order[]>(`api/orders?judgeId=${judgeId ?? ''}`);
  }
}
