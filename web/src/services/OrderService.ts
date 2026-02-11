import { Order, OrderReview } from '@/types';
import { IHttpService } from './HttpService';
import { ServiceBase } from './ServiceBase';

export class OrderService extends ServiceBase {
  private readonly baseUrl: string = 'api/orders';

  constructor(httpService: IHttpService) {
    super(httpService);
  }

  review = (orderId: string, review: FormData): Promise<void> => {
    return this.httpService.patchOG(`${this.baseUrl}/${orderId}/review`, review, { 'Content-Type': 'undefined' });
  };

  getOrders(judgeId: number | null = null): Promise<Order[]> {
    return this.httpService.get<Order[]>(`api/orders?judgeId=232`);
  }
}