using System;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using ClassLibrary2;

namespace Diplom
{
    public class BdClass
    {
        public int AddRoute(string code)
        {
            using (var db = new dataBase())
            {
                Route route = new Route
                {
                    code = code
                };

                db.Route.Add(route);
                db.SaveChanges();

                return route.id;
            }
        }

        public int AddDirection(int routeId, string directionType)
        {
            using (var db = new dataBase())
            {
                var route = db.Route.FirstOrDefault(r => r.id == routeId);
                if (route == null)
                    throw new Exception("Маршрут не найден.");

                Direction direction = new Direction
                {
                    route_id = routeId,
                    direction_type = directionType
                };

                db.Direction.Add(direction);
                db.SaveChanges();

                return direction.id;
            }
        }

        public int AddPicket(int directionId, int picketNumber, string description)
        {
            using (var db = new dataBase())
            {
                var direction = db.Direction.FirstOrDefault(d => d.id == directionId);
                if (direction == null)
                    throw new Exception("Направление не найдено.");

                Picket picket = new Picket
                {
                    direction_id = directionId,
                    picket_number = picketNumber,
                    description = description
                };

                db.Picket.Add(picket);
                db.SaveChanges();

                return picket.id;
            }
        }

        public int CreatePath(string routeCode, string directionType, int picketNumber, string description = null)
        {
            try
            {
                using (var db = new dataBase())
                {
                    Route route = new Route
                    {
                        code = routeCode
                    };
                    db.Route.Add(route);
                    db.SaveChanges();

                    Direction direction = new Direction
                    {
                        route_id = route.id,
                        direction_type = directionType
                    };
                    db.Direction.Add(direction);
                    db.SaveChanges();

                    Picket picket = new Picket
                    {
                        direction_id = direction.id,
                        picket_number = picketNumber,
                        description = description
                    };
                    db.Picket.Add(picket);
                    db.SaveChanges();

                    return picket.id;
                }
            }
            catch (DbEntityValidationException ex)
            {
                StringBuilder sb = new StringBuilder();

                foreach (var eve in ex.EntityValidationErrors)
                {
                    foreach (var ve in eve.ValidationErrors)
                    {
                        sb.AppendLine(ve.PropertyName + ": " + ve.ErrorMessage);
                    }
                }

                throw new Exception("Ошибка валидации Entity Framework:\n" + sb.ToString());
            }
            catch (Exception ex)
            {
                string msg = ex.Message;

                if (ex.InnerException != null)
                    msg += "\nINNER: " + ex.InnerException.Message;

                if (ex.InnerException != null && ex.InnerException.InnerException != null)
                    msg += "\nINNER 2: " + ex.InnerException.InnerException.Message;

                throw new Exception(msg);
            }
        }
        public int GetRouteIdByCurrentValues()
        {
            using (var db = new dataBase())
            {
                var route = db.Route.FirstOrDefault(r => r.code == BdReg.CurrentRoute);

                if (route == null)
                    throw new Exception("Route не найден.");

                return route.id;
            }
        }

        public int GetDirectionIdByCurrentValues()
        {
            using (var db = new dataBase())
            {
                var route = db.Route.FirstOrDefault(r => r.code == BdReg.CurrentRoute);
                if (route == null)
                    throw new Exception("Route не найден.");

                var direction = db.Direction.FirstOrDefault(d =>
                    d.route_id == route.id &&
                    d.direction_type == BdReg.CurrentDirection);

                if (direction == null)
                    throw new Exception("Direction не найден.");

                return direction.id;
            }
        }

        public int GetPicketIdByCurrentValues()
        {
            using (var db = new dataBase())
            {
                var route = db.Route.FirstOrDefault(r => r.code == BdReg.CurrentRoute);
                if (route == null)
                    throw new Exception("Route не найден.");

                var direction = db.Direction.FirstOrDefault(d =>
                    d.route_id == route.id &&
                    d.direction_type == BdReg.CurrentDirection);

                if (direction == null)
                    throw new Exception("Direction не найден.");

                int picketNumber;
                if (!int.TryParse(BdReg.CurrentPicket, out picketNumber))
                    throw new Exception("CurrentPicket не является числом.");

                var picket = db.Picket.FirstOrDefault(p =>
                    p.direction_id == direction.id &&
                    p.picket_number == picketNumber);

                if (picket == null)
                    throw new Exception("Picket не найден.");

                return picket.id;
            }
        }

        public int AddSignPlacementToCurrentPath(
    int signId,
    double? distanceM,
    string bermCondition,
    double? signHeight,
    string signCondition,
    int? visibilityPercent,
    string comment,
    byte[] photo)
        {
            if (!BdReg.CurrentPicketId.HasValue)
                throw new Exception("Текущий Picket не выбран.");

            using (var db = new dataBase())
            {
                int picketId = BdReg.CurrentPicketId.Value;

                var picket = db.Picket.FirstOrDefault(p => p.id == picketId);
                if (picket == null)
                    throw new Exception("Picket не найден.");

                SignPlacement signPlacement = new SignPlacement
                {
                    picket_id = picketId,
                    sign_id = signId,
                    distance_m = distanceM,
                    berm_condition = bermCondition,
                    sign_height = signHeight,
                    sign_condition = signCondition,
                    visibility_percent = visibilityPercent,
                    comment = comment,
                    photo = photo
                };

                db.SignPlacement.Add(signPlacement);
                db.SaveChanges();

                return signPlacement.id;
            }
        }


        public int GetRouteIdByCode(string routeCode)
        {
            using (var db = new dataBase())
            {
                var route = db.Route.FirstOrDefault(r => r.code == routeCode);

                if (route == null)
                    throw new Exception("Route не найден.");

                return route.id;
            }
        }

        public int GetDirectionIdByRouteAndName(int routeId, string directionType)
        {
            using (var db = new dataBase())
            {
                var direction = db.Direction.FirstOrDefault(d =>
                    d.route_id == routeId &&
                    d.direction_type == directionType);

                if (direction == null)
                    throw new Exception("Direction не найден.");

                return direction.id;
            }
        }

        public int GetPicketIdByDirectionAndNumber(int directionId, int picketNumber)
        {
            using (var db = new dataBase())
            {
                var picket = db.Picket.FirstOrDefault(p =>
                    p.direction_id == directionId &&
                    p.picket_number == picketNumber);

                if (picket == null)
                    throw new Exception("Picket не найден.");

                return picket.id;
            }
        }

    }
}


        
